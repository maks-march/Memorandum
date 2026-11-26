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

function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

async function ensureCsrfToken(): Promise<string | null> {
  if (cachedCsrfToken) return cachedCsrfToken;

  const cookieToken = readCookie('XSRF-TOKEN');
  if (cookieToken) {
    cachedCsrfToken = cookieToken;
    return cookieToken;
  }

  try {
    const res = await fetch('/api/v1/auth/csrf', {
      method: 'GET',
      credentials: 'include',
      headers: { Accept: 'application/json' },
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });
    if (!res.ok) return null;
    const data = (await res.json()) as { xsrfToken?: string; token?: string };
    const token = data.xsrfToken || data.token;
    if (token) {
      cachedCsrfToken = token;
      return token;
    }
  } catch {
    return null;
  }
  return null;
}

function extractProblemError(problem: ProblemDetails, status: number): {
  error: string;
  fieldErrors?: Record<string, string[]>;
} {
  if (problem.errors) {
    const firstMsg = Object.values(problem.errors).flat()[0];
    if (firstMsg) {
      return { error: firstMsg, fieldErrors: problem.errors };
    }
  }
  if (problem.detail) return { error: problem.detail };
  if (problem.title) return { error: problem.title };
  return { error: `Ошибка сервера (HTTP ${status})` };
}

async function postAuthJson<TResponse>(path: string, body: unknown): Promise<ApiResult<TResponse>> {
  try {
    const csrf = await ensureCsrfToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    };
    if (csrf) {
      headers['X-XSRF-TOKEN'] = csrf;
    }

    const res = await fetch(path, {
      method: 'POST',
      credentials: 'include',
      headers,
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });

    if (!res.ok) {
      if (res.status === 400 || res.status === 401 || res.status === 409 || res.status === 422 || res.status === 429) {
        try {
          const problem = (await res.json()) as ProblemDetails;
          const parsed = extractProblemError(problem, res.status);
          return { ok: false, status: res.status, ...parsed };
        } catch {
          return { ok: false, status: res.status, error: `Ошибка запроса (HTTP ${res.status})` };
        }
      }
      if (res.status === 404) {
        return { ok: false, status: 404, error: 'Метод API ещё не реализован на сервере (404)' };
      }
      return { ok: false, status: res.status, error: `Ошибка сервера (HTTP ${res.status})` };
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

export async function loginUser(email: string, password: string): Promise<ApiResult<LoginResponse>> {
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
  return postAuthJson<void>('/api/v1/auth/logout', {});
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

  try {
    let res = await fetch(url, {
      method: 'GET',
      credentials: 'include',
      headers: { Accept: 'application/json' },
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });

    // Если основной контроллер еще не настроен на сервере, проверяем контроллер заглушек бэкенда
    if (res.status === 404) {
      res = await fetch('/api/v1/mocks/projects', {
        method: 'GET',
        credentials: 'include',
        headers: { Accept: 'application/json' },
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });
    }

    if (!res.ok) {
      if (res.status === 401) {
        return { items: [], error: 'Необходима авторизация для просмотра проектов' };
      }
      return { items: [], error: `Ошибка сервера (HTTP ${res.status})` };
    }

    const rawData = (await res.json()) as
      | PagedBackendResponse<BackendProjectDto>
      | BackendProjectDto[];

    const rawList: BackendProjectDto[] = Array.isArray(rawData)
      ? rawData
      : Array.isArray(rawData?.items)
        ? rawData.items
        : [];

    const mapped: ProjectItem[] = rawList.map((p) => {
      const d = new Date(p.createdAt || p.updatedAt || Date.now());
      const formattedDate = !isNaN(d.getTime())
        ? `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${d.getFullYear()}`
        : '26.11.2026';

      const workType =
        p.workType === 'Construction'
          ? 'Строительство'
          : p.workType === 'Reconstruction'
            ? 'Реконструкция'
            : p.workType || 'Строительство';

      const responsibleName =
        p.responsible?.fullName ||
        (p.responsibleUserId ? `Сотрудник #${p.responsibleUserId}` : 'Не назначен');

      return {
        id: p.id,
        name: p.name,
        objectName: p.objectName || 'Объект капитального строительства',
        addressText: p.addressText || 'Не указан',
        workType,
        responsibleName,
        responsibleUserId: p.responsible?.id ?? p.responsibleUserId ?? null,
        createdAt: formattedDate,
        isArchived: p.isArchived,
        documentsCount: p.documentsCount,
        code: p.code || undefined,
      };
    });

    return { items: mapped };
  } catch {
    return { items: [], error: 'Не удалось подключиться к серверу API' };
  }
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
    workType:
      input.workType === 'Строительство'
        ? 'Construction'
        : input.workType === 'Реконструкция'
          ? 'Reconstruction'
          : input.workType,
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
