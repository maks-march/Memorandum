export type Health = {
  status: string;
  mode: string;
  environment: string;
  database: string;
  version: string;
  machine: string;
  timeUtc: string;
  requestId?: string;
};

export type HealthResult =
  | { ok: true; data: Health; ms: number; url: string }
  | { ok: false; error: string; status?: number; ms: number; url: string };

export type UserProfile = {
  id: number;
  email: string;
  fullName: string;
  phone: string | null;
  position: string | null;
  organizationId: number | null;
  organizationName: string | null;
  role: string | null;
};

export type LoginResponse = {
  user: UserProfile;
  xsrfToken?: string;
  csrfToken?: string;
};

export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: string; status?: number; fieldErrors?: Record<string, string[]> };

const TIMEOUT_MS = 8000;
let cachedCsrfToken: string | null = null;
let csrfRefreshPromise: Promise<string | null> | null = null;

/** Запрос health по относительному пути — работает и в dev (Vite proxy), и в prod (nginx). */
export async function fetchHealth(path: string, init?: RequestInit): Promise<HealthResult> {
  const started = performance.now();
  const url = path;
  try {
    const res = await fetch(path, {
      headers: { Accept: 'application/json' },
      ...init,
      signal: init?.signal ?? AbortSignal.timeout(TIMEOUT_MS),
    });
    const ms = Math.round(performance.now() - started);

    if (!res.ok) {
      const body = await res.text();
      return { ok: false, error: `HTTP ${res.status}: ${body.slice(0, 300)}`, status: res.status, ms, url };
    }
    return { ok: true, data: (await res.json()) as Health, ms, url };
  } catch (e) {
    const isTimeout = e instanceof DOMException && e.name === 'TimeoutError';
    return {
      ok: false,
      error: isTimeout
        ? `Таймаут запроса (${TIMEOUT_MS} мс)`
        : e instanceof Error
          ? e.message
          : String(e),
      ms: Math.round(performance.now() - started),
      url,
    };
  }
}

async function ensureCsrfToken(): Promise<string | null> {
  if (cachedCsrfToken) return cachedCsrfToken;
  if (csrfRefreshPromise) return csrfRefreshPromise;

  csrfRefreshPromise = (async () => {
    try {
      const res = await fetch('/api/v1/auth/csrf', {
        method: 'GET',
        credentials: 'include',
        headers: { Accept: 'application/json' },
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });
      if (!res.ok) return null;
      const data = (await res.json()) as { xsrfToken?: string; token?: string };
      const token = data.xsrfToken || data.token || null;
      if (token) {
        cachedCsrfToken = token;
      }
      return token;
    } catch {
      return null;
    } finally {
      csrfRefreshPromise = null;
    }
  })();

  return csrfRefreshPromise;
}

type ProblemDetailsWithExtensions = ProblemDetails & {
  detectedSchema?: string;
  detectedSchemaCode?: string;
};

function extractProblemError(problem: ProblemDetailsWithExtensions, status: number): {
  error: string;
  fieldErrors?: Record<string, string[]>;
  problem?: ProblemDetailsWithExtensions;
} {
  if (problem.errors) {
    const firstMsg = Object.values(problem.errors).flat()[0];
    if (firstMsg) {
      return { error: firstMsg, fieldErrors: problem.errors, problem };
    }
  }
  if (problem.detail) return { error: problem.detail, problem };
  if (problem.title) return { error: problem.title, problem };
  return { error: `Ошибка сервера (HTTP ${status})`, problem };
}

type MutationErrorExtras = {
  problem?: ProblemDetailsWithExtensions;
};

async function parseErrorResult(res: Response): Promise<{
  ok: false;
  status: number;
  error: string;
  fieldErrors?: Record<string, string[]>;
} & MutationErrorExtras> {
  const contentType = res.headers.get('content-type') || '';
  if (contentType.includes('json')) {
    try {
      const problem = (await res.json()) as ProblemDetailsWithExtensions;
      const parsed = extractProblemError(problem, res.status);
      return { ok: false, status: res.status, ...parsed };
    } catch {
      // Fallback below when body isn't valid JSON
    }
  }

  if (res.status === 404) {
    return { ok: false, status: 404, error: 'Метод API ещё не реализован на сервере (404)' };
  }
  if (res.status === 415) {
    return {
      ok: false,
      status: 415,
      error: 'Версия схемы не поддерживается',
    };
  }
  if (res.status === 401) {
    return { ok: false, status: 401, error: 'Необходима авторизация' };
  }

  return { ok: false, status: res.status, error: `Ошибка сервера (HTTP ${res.status})` };
}

async function sendMutation<TResponse>(
  path: string,
  options: {
    method?: 'POST' | 'PATCH' | 'DELETE';
    body?: BodyInit | null;
    jsonBody?: unknown;
    timeoutMs?: number;
  } = {}
): Promise<ApiResult<TResponse> & MutationErrorExtras> {
  try {
    const csrf = await ensureCsrfToken();
    const headers: Record<string, string> = {
      Accept: 'application/json',
    };
    if (csrf) {
      headers['X-XSRF-TOKEN'] = csrf;
    }

    let requestBody: BodyInit | undefined;
    if (options.jsonBody !== undefined) {
      headers['Content-Type'] = 'application/json';
      requestBody = JSON.stringify(options.jsonBody);
    } else if (options.body !== undefined && options.body !== null) {
      requestBody = options.body;
    }

    const res = await fetch(path, {
      method: options.method ?? 'POST',
      credentials: 'include',
      headers,
      body: requestBody,
      signal: AbortSignal.timeout(options.timeoutMs ?? TIMEOUT_MS),
    });

    if (!res.ok) {
      return await parseErrorResult(res);
    }

    if (res.status === 204) {
      return { ok: true, data: undefined as TResponse };
    }

    const data = (await res.json()) as TResponse;
    return { ok: true, data };
  } catch (e) {
    const isTimeout = e instanceof DOMException && e.name === 'TimeoutError';
    return {
      ok: false,
      error: isTimeout
        ? 'Превышено время ожидания ответа сервера'
        : 'Не удалось подключиться к серверу',
    };
  }
}

async function postAuthJson<TResponse>(path: string, body: unknown): Promise<ApiResult<TResponse>> {
  return sendMutation<TResponse>(path, { method: 'POST', jsonBody: body });
}

export async function loginUser(email: string, password: string): Promise<ApiResult<LoginResponse>> {
  cachedCsrfToken = null;
  const result = await postAuthJson<LoginResponse>('/api/v1/auth/login', { email, password });
  if (result.ok) {
    const nextToken = result.data.xsrfToken || result.data.csrfToken;
    if (nextToken) {
      cachedCsrfToken = nextToken;
    }
  }
  return result;
}

export async function registerWithOrg(params: {
  email: string;
  password: string;
  fullName: string;
  orgName: string;
}): Promise<ApiResult<UserProfile>> {
  return postAuthJson<UserProfile>('/api/v1/auth/register', params);
}

export async function registerWithInvite(params: {
  code: string;
  email: string;
  password: string;
  fullName: string;
}): Promise<ApiResult<UserProfile>> {
  return postAuthJson<UserProfile>('/api/v1/auth/accept-invite', params);
}

export async function requestPasswordReset(email: string): Promise<ApiResult<void>> {
  // Поддержка нового эндпоинта POST /api/v1/auth/change-password из origin/main
  const res = await postAuthJson<void>('/api/v1/auth/change-password', { email });
  if (!res.ok && res.status === 404) {
    return postAuthJson<void>('/api/v1/auth/forgot-password', { email });
  }
  return res;
}

export async function confirmPasswordReset(token: string, newPassword: string): Promise<ApiResult<void>> {
  return postAuthJson<void>('/api/v1/auth/reset-password', { token, newPassword });
}

export async function logoutUser(): Promise<ApiResult<void>> {
  const res = await postAuthJson<void>('/api/v1/auth/logout', {});
  cachedCsrfToken = null;
  return res;
}

// -------------------------------------------------------------
// ТИПЫ И ИНТЕГРАЦИЯ ДЛЯ ПРОЕКТОВ И ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ (БЭКЕНД)
// -------------------------------------------------------------

export interface ProjectItem {
  id: number;
  name: string;
  objectName: string;
  addressText: string;
  workType: string;
  responsibleName: string;
  responsibleUserId?: number | null;
  createdAt: string;
  isArchived?: boolean;
  documentsCount?: number;
  code?: string | null;
}

export interface CreateProjectInput {
  name: string;
  objectName: string;
  addressText: string;
  workType: string;
  responsibleUserId?: number | null;
  responsibleName?: string;
  organizationName?: string;
  createdAt?: string;
  code?: string | null;
}

interface BackendUserRef {
  id: number;
  fullName: string;
}

interface BackendProjectDto {
  id: number;
  name: string;
  objectName: string;
  workType?: string | null;
  addressText?: string | null;
  code?: string | null;
  responsible?: BackendUserRef | null;
  responsibleUserId?: number | null;
  isArchived: boolean;
  documentsCount: number;
  createdAt?: string;
  updatedAt?: string;
}

interface PagedBackendResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

const WORK_TYPE_LABELS: Record<string, string> = {
  Construction: 'Строительство',
  Reconstruction: 'Реконструкция',
};

const WORK_TYPE_CODES: Record<string, string> = {
  Строительство: 'Construction',
  Реконструкция: 'Reconstruction',
};

function formatDateRu(raw?: string): string {
  const d = new Date(raw || Date.now());
  if (isNaN(d.getTime())) return '—';
  return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${d.getFullYear()}`;
}

function mapBackendProject(p: BackendProjectDto): ProjectItem {
  const responsibleName =
    p.responsible?.fullName ||
    (p.responsibleUserId ? `Сотрудник #${p.responsibleUserId}` : 'Не назначен');

  return {
    id: p.id,
    name: p.name,
    objectName: p.objectName || 'Объект капитального строительства',
    addressText: p.addressText || 'Не указан',
    workType: (p.workType && WORK_TYPE_LABELS[p.workType]) || p.workType || 'Строительство',
    responsibleName,
    responsibleUserId: p.responsible?.id ?? p.responsibleUserId ?? null,
    createdAt: formatDateRu(p.createdAt || p.updatedAt),
    isArchived: p.isArchived,
    documentsCount: p.documentsCount,
    code: p.code || undefined,
  };
}

function unwrapList<T>(rawData: PagedBackendResponse<T> | T[]): { items: T[]; total: number } {
  if (Array.isArray(rawData)) {
    return { items: rawData, total: rawData.length };
  }
  const items = Array.isArray(rawData?.items) ? rawData.items : [];
  return { items, total: typeof rawData?.total === 'number' ? rawData.total : items.length };
}

async function fetchJsonWithFallback<T>(
  urls: string[]
): Promise<{ ok: true; data: T; usedFallback: boolean } | { ok: false; error: string; status?: number }> {
  try {
    let lastStatus: number | undefined;

    for (let i = 0; i < urls.length; i++) {
      const res = await fetch(urls[i], {
        method: 'GET',
        credentials: 'include',
        headers: { Accept: 'application/json' },
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });

      if (res.status === 404 && i < urls.length - 1) {
        lastStatus = 404;
        continue;
      }

      if (!res.ok) {
        lastStatus = res.status;
        const parsed = await parseErrorResult(res);
        return { ok: false, error: parsed.error, status: res.status };
      }

      const data = (await res.json()) as T;
      return { ok: true, data, usedFallback: i > 0 };
    }

    return { ok: false, error: `Ошибка сервера (HTTP ${lastStatus ?? 404})`, status: lastStatus };
  } catch (e) {
    const isTimeout = e instanceof DOMException && e.name === 'TimeoutError';
    return {
      ok: false,
      error: isTimeout ? 'Превышено время ожидания ответа сервера' : 'Не удалось подключиться к серверу API',
    };
  }
}

/** Получить текущего пользователя строго из бэкенда (GET /api/v1/me) */
export async function getCurrentUser(): Promise<UserProfile | null> {
  try {
    const res = await fetch('/api/v1/me', {
      method: 'GET',
      credentials: 'include',
      headers: { Accept: 'application/json' },
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });

    if (res.ok) {
      return (await res.json()) as UserProfile;
    }
  } catch {
    // Сеть недоступна или нет авторизации
  }

  return null;
}

/** Получить список проектов из бэкенда (GET /api/v1/projects?q=...) */
export async function getProjects(params?: {
  q?: string;
  workType?: string;
  archived?: boolean;
}): Promise<{ items: ProjectItem[]; error?: string }> {
  const query = new URLSearchParams();
  if (params?.q?.trim()) query.set('q', params.q.trim());
  if (params?.archived !== undefined) query.set('archived', String(params.archived));
  const queryString = query.toString();
  const url = queryString ? `/api/v1/projects?${queryString}` : '/api/v1/projects';

  const result = await fetchJsonWithFallback<PagedBackendResponse<BackendProjectDto> | BackendProjectDto[]>([
    url,
    '/api/v1/mocks/projects',
  ]);

  if (!result.ok) {
    const errorMsg =
      result.status === 401
        ? 'Необходима авторизация для просмотра проектов'
        : result.error;
    return { items: [], error: errorMsg };
  }

  const { items } = unwrapList(result.data);
  return { items: items.map(mapBackendProject) };
}

/** Получить карточку проекта (GET /api/v1/projects/{id}) */
export async function getProjectById(projectId: number): Promise<ApiResult<ProjectItem>> {
  const result = await fetchJsonWithFallback<
    { project: BackendProjectDto } | BackendProjectDto
  >([`/api/v1/projects/${projectId}`, `/api/v1/mocks/projects/${projectId}`]);

  if (!result.ok) {
    return result;
  }

  const dto = 'project' in result.data ? result.data.project : result.data;
  return { ok: true, data: mapBackendProject(dto) };
}

/** Создать новый проект на бэкенде (POST /api/v1/projects) */
export async function createProject(
  input: CreateProjectInput
): Promise<ApiResult<ProjectItem>> {
  const body = {
    name: input.name,
    objectName: input.objectName,
    objectKind: 'residential',
    addressText: input.addressText,
    workType: WORK_TYPE_CODES[input.workType] || input.workType,
    responsibleUserId: input.responsibleUserId || null,
    code: input.code || null,
  };

  const res = await postAuthJson<{ id: number; name: string }>('/api/v1/projects', body);
  if (!res.ok) {
    return res;
  }

  const newProject: ProjectItem = {
    id: res.data?.id || Date.now(),
    name: input.name,
    objectName: input.objectName,
    addressText: input.addressText,
    workType: input.workType,
    responsibleName: input.responsibleName || 'Не назначен',
    responsibleUserId: input.responsibleUserId || null,
    createdAt: input.createdAt || new Date().toLocaleDateString('ru-RU'),
    isArchived: false,
    documentsCount: 0,
    code: input.code,
  };

  return { ok: true, data: newProject };
}

// -------------------------------------------------------------
// ТИПЫ И ИНТЕГРАЦИЯ ДЛЯ ДОКУМЕНТОВ (ПОЯСНИТЕЛЬНЫХ ЗАПИСОК) И СХЕМ XML
// -------------------------------------------------------------

export type DocumentStatus = 'Draft' | 'Verified' | 'HasErrors' | 'Archived';

export interface DocumentItem {
  id: number;
  projectId: number;
  projectName: string;
  title: string;
  organizationName: string;
  author: string | null;
  status: DocumentStatus;
  schemaCode: string;
  updatedAt: string;
}

export interface SchemaVersionItem {
  code: string;
  title: string;
  effectiveFrom: string;
  isDefault: boolean;
}

export interface ImportWarningItem {
  path: string;
  reason: string;
}

export interface ImportDocumentResult {
  documentId: number;
  detectedSchema: string;
  importedFields: number;
  warnings: ImportWarningItem[];
  originalFileId?: number | null;
}

interface BackendDocumentDto {
  id: number;
  projectId: number;
  projectName: string;
  title: string;
  organizationName: string;
  author?: string | null;
  status: string;
  schemaCode: string;
  updatedAt: string;
}

interface BackendImportDto {
  documentId?: number;
  id?: number;
  detectedSchema?: string | null;
  detectedSchemaCode?: string | null;
  importedFields?: number;
  warnings?: ImportWarningItem[];
  originalFileId?: number | null;
}

function normalizeDocumentStatus(status: string): DocumentStatus {
  switch (status.toLowerCase()) {
    case 'verified':
      return 'Verified';
    case 'haserrors':
    case 'has_errors':
      return 'HasErrors';
    case 'archived':
      return 'Archived';
    default:
      return 'Draft';
  }
}

function mapBackendDocument(dto: BackendDocumentDto): DocumentItem {
  return {
    id: dto.id,
    projectId: dto.projectId,
    projectName: dto.projectName,
    title: dto.title,
    organizationName: dto.organizationName,
    author: dto.author ?? null,
    status: normalizeDocumentStatus(dto.status),
    schemaCode: dto.schemaCode,
    updatedAt: dto.updatedAt,
  };
}

/** Получить список пояснительных записок (GET /api/v1/documents или /api/v1/projects/{id}/documents) */
export async function getDocuments(params?: {
  projectId?: number;
  q?: string;
  status?: string;
  schemaCode?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}): Promise<{ items: DocumentItem[]; total: number; fromMockFallback: boolean; error?: string }> {
  const query = new URLSearchParams();
  if (params?.q?.trim()) query.set('q', params.q.trim());
  if (params?.projectId !== undefined) query.set('projectId', String(params.projectId));
  if (params?.status && params.status !== 'all') query.set('status', params.status);
  if (params?.schemaCode && params.schemaCode !== 'all') query.set('schemaCode', params.schemaCode);
  if (params?.sort) query.set('sort', params.sort);
  if (params?.page !== undefined) query.set('page', String(params.page));
  if (params?.pageSize !== undefined) query.set('pageSize', String(params.pageSize));

  const qs = query.toString();
  const primaryUrl = qs ? `/api/v1/documents?${qs}` : '/api/v1/documents';

  const candidateUrls: string[] = [primaryUrl];
  if (params?.projectId !== undefined) {
    const projectQs = new URLSearchParams(query);
    projectQs.delete('projectId');
    const pQsString = projectQs.toString();
    candidateUrls.push(
      pQsString
        ? `/api/v1/projects/${params.projectId}/documents?${pQsString}`
        : `/api/v1/projects/${params.projectId}/documents`,
      `/api/v1/mocks/projects/${params.projectId}/documents`
    );
  } else {
    candidateUrls.push('/api/v1/mocks/documents');
  }

  const result = await fetchJsonWithFallback<
    PagedBackendResponse<BackendDocumentDto> | BackendDocumentDto[]
  >(candidateUrls);

  if (!result.ok) {
    const errorMsg =
      result.status === 401
        ? 'Необходима авторизация для просмотра документов'
        : result.error;
    return { items: [], total: 0, fromMockFallback: false, error: errorMsg };
  }

  const { items, total } = unwrapList(result.data);
  return {
    items: items.map(mapBackendDocument),
    total,
    fromMockFallback: result.usedFallback,
  };
}

/** Получить активные версии XML-схем (GET /api/v1/schema-versions) */
export async function getSchemaVersions(): Promise<ApiResult<SchemaVersionItem[]>> {
  const result = await fetchJsonWithFallback<
    SchemaVersionItem[] | PagedBackendResponse<SchemaVersionItem>
  >(['/api/v1/schema-versions', '/api/v1/mocks/schema-versions']);

  if (!result.ok) {
    return result;
  }

  const { items } = unwrapList(result.data);
  return { ok: true, data: items };
}

/** Создать новую пояснительную записку (POST /api/v1/projects/{projectId}/documents) */
export async function createDocument(
  projectId: number,
  input: { title: string; schemaCode: string }
): Promise<ApiResult<{ id: number; title: string; schemaCode: string }>> {
  const res = await postAuthJson<{
    id?: number;
    documentId?: number;
    title?: string;
    schemaCode?: string;
  }>(`/api/v1/projects/${projectId}/documents`, {
    title: input.title,
    schemaCode: input.schemaCode,
  });

  if (!res.ok) {
    return res;
  }

  return {
    ok: true,
    data: {
      id: res.data?.id ?? res.data?.documentId ?? 0,
      title: res.data?.title ?? input.title,
      schemaCode: res.data?.schemaCode ?? input.schemaCode,
    },
  };
}

export type ImportDocumentError = {
  ok: false;
  status?: number;
  error: string;
  detail?: string;
  detectedSchema?: string;
};

/** Импортировать XML в проект (POST /api/v1/projects/{projectId}/documents/import) */
export async function importDocumentXml(
  projectId: number,
  file: File,
  title?: string
): Promise<{ ok: true; data: ImportDocumentResult } | ImportDocumentError> {
  const formData = new FormData();
  formData.append('file', file);
  if (title?.trim()) {
    formData.append('title', title.trim());
  }

  const res = await sendMutation<BackendImportDto>(
    `/api/v1/projects/${projectId}/documents/import`,
    {
      method: 'POST',
      body: formData,
      timeoutMs: 30000,
    }
  );

  if (!res.ok) {
    const problem = res.problem;
    return {
      ok: false,
      status: res.status,
      error: problem?.title || res.error,
      detail: problem?.detail,
      detectedSchema: problem?.detectedSchema || problem?.detectedSchemaCode,
    };
  }

  return {
    ok: true,
    data: {
      documentId: res.data?.documentId ?? res.data?.id ?? 0,
      detectedSchema: res.data?.detectedSchema ?? res.data?.detectedSchemaCode ?? '',
      importedFields: res.data?.importedFields ?? 0,
      warnings: Array.isArray(res.data?.warnings) ? res.data.warnings : [],
      originalFileId: res.data?.originalFileId ?? null,
    },
  };
}

/** Архивировать / восстановить документ */
export async function archiveDocument(documentId: number): Promise<ApiResult<void>> {
  return postAuthJson<void>(`/api/v1/documents/${documentId}/archive`, {});
}

export async function unarchiveDocument(documentId: number): Promise<ApiResult<void>> {
  return postAuthJson<void>(`/api/v1/documents/${documentId}/unarchive`, {});
}
