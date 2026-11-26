// frontend/src/pages/DocumentsPage/DocumentsPage.tsx
import React, { useEffect, useId, useMemo, useRef, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  archiveDocument,
  getDocuments,
  getSchemaVersions,
  unarchiveDocument,
  type DocumentItem,
  type DocumentStatus,
} from '@/api';
import {
  formatDocumentDate,
  STATUS_LABELS,
  STATUS_SORT_ORDER,
  useProjectTitle,
} from './documentHelpers';
import styles from './DocumentsPage.module.css';

const STATUS_STYLE_CLASS: Record<DocumentStatus, string> = {
  Draft: styles.statusDraft,
  Verified: styles.statusVerified,
  HasErrors: styles.statusHasErrors,
  Archived: styles.statusArchived,
};

type LocationState = {
  projectName?: string;
} | null;

/**
 * Экран списка пояснительных записок (строго по макетам Figma):
 * - Без projectId: «Все документы» (/documents)
 * - С projectId: «Документы проекта» (/projects/:projectId/documents)
 *   с хлебными крошками и кнопками «Импорт XML» / «Создать записку»
 */
export const DocumentsPage: React.FC = () => {
  const { projectId: projectIdParam } = useParams<{ projectId?: string }>();
  const location = useLocation();
  const navigate = useNavigate();

  const numericProjectId =
    projectIdParam !== undefined ? Number(projectIdParam) : undefined;
  const isProjectView =
    numericProjectId !== undefined && !Number.isNaN(numericProjectId);

  const locationState = location.state as LocationState;
  const projectTitle = useProjectTitle(
    isProjectView ? numericProjectId : undefined,
    locationState?.projectName
  );

  const [documents, setDocuments] = useState<DocumentItem[]>([]);
  const [fromMockFallback, setFromMockFallback] = useState(false);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [schemaOptionsFromApi, setSchemaOptionsFromApi] = useState<string[]>([]);

  // Фильтры и сортировка
  const [searchQuery, setSearchQuery] = useState('');
  const [organizationFilter, setOrganizationFilter] = useState('all');
  const [authorFilter, setAuthorFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [schemaFilter, setSchemaFilter] = useState('all');
  const [sortBy, setSortBy] = useState('-updatedAt');

  // Открытое контекстное меню строки (`...`)
  const [openMenuDocId, setOpenMenuDocId] = useState<number | null>(null);
  const menuAnchorRef = useRef<HTMLTableCellElement | null>(null);

  const searchInputId = useId();
  const sortSelectId = useId();
  const titleId = useId();

  // Сброс фильтров при переключении между «Все документы» и «Документы проекта»
  useEffect(() => {
    setOrganizationFilter('all');
    setAuthorFilter('all');
    setOpenMenuDocId(null);
    setActionError(null);
  }, [numericProjectId]);

  // Загружаем версии схем для выпадающего списка фильтра «Схема XML»
  useEffect(() => {
    let isMounted = true;
    getSchemaVersions().then((res) => {
      if (isMounted && res.ok) {
        setSchemaOptionsFromApi(res.data.map((s) => s.code));
      }
    });
    return () => {
      isMounted = false;
    };
  }, []);

  // Запрос документов с сервера при изменении проекта, поиска, статуса, схемы или сортировки
  useEffect(() => {
    let isMounted = true;
    const timeoutId = setTimeout(() => {
      getDocuments({
        projectId: isProjectView ? numericProjectId : undefined,
        q: searchQuery,
        status: statusFilter,
        schemaCode: schemaFilter,
        sort: sortBy,
      }).then(({ items, fromMockFallback: usedFallback, error }) => {
        if (isMounted) {
          setDocuments(items);
          setFromMockFallback(usedFallback);
          setErrorMessage(error || null);
          setLoading(false);
        }
      });
    }, 120);

    return () => {
      isMounted = false;
      clearTimeout(timeoutId);
    };
  }, [isProjectView, numericProjectId, searchQuery, statusFilter, schemaFilter, sortBy]);

  // Закрытие меню строки по клику вне меню и по клавише Escape
  useEffect(() => {
    if (openMenuDocId === null) return;

    const handleOutsideClick = (event: MouseEvent) => {
      if (
        menuAnchorRef.current &&
        !menuAnchorRef.current.contains(event.target as Node)
      ) {
        setOpenMenuDocId(null);
      }
    };

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpenMenuDocId(null);
      }
    };

    document.addEventListener('mousedown', handleOutsideClick);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleOutsideClick);
      document.removeEventListener('keydown', handleEscape);
    };
  }, [openMenuDocId]);

  // Динамические опции для селектов из реально полученных документов
  const organizationOptions = useMemo(() => {
    const unique = new Set<string>();
    documents.forEach((doc) => {
      if (doc.organizationName) unique.add(doc.organizationName);
    });
    return Array.from(unique).sort((a, b) => a.localeCompare(b, 'ru'));
  }, [documents]);

  const authorOptions = useMemo(() => {
    const unique = new Set<string>();
    documents.forEach((doc) => {
      if (doc.author) unique.add(doc.author);
    });
    return Array.from(unique).sort((a, b) => a.localeCompare(b, 'ru'));
  }, [documents]);

  const schemaOptions = useMemo(() => {
    const unique = new Set<string>(schemaOptionsFromApi);
    documents.forEach((doc) => {
      if (doc.schemaCode) unique.add(doc.schemaCode);
    });
    return Array.from(unique).sort((a, b) => b.localeCompare(a, 'ru'));
  }, [documents, schemaOptionsFromApi]);

  // Фильтрация по полям, не имеющим отдельного query-фильтра (Организация / Автор),
  // а также клиентское применение фильтров/сортировки если ответ пришел от dev-заглушки
  const visibleDocuments = useMemo(() => {
    const queryLower = searchQuery.trim().toLowerCase();

    const filtered = documents.filter((doc) => {
      if (organizationFilter !== 'all' && doc.organizationName !== organizationFilter) {
        return false;
      }
      if (authorFilter !== 'all' && doc.author !== authorFilter) {
        return false;
      }

      if (fromMockFallback) {
        if (isProjectView && doc.projectId !== numericProjectId) {
          // Если dev-заглушка вернула общий список без учета id проекта — оставляем элементы этого проекта если они есть
          const hasMatchingProjectDocs = documents.some(
            (item) => item.projectId === numericProjectId
          );
          if (hasMatchingProjectDocs) return false;
        }
        if (statusFilter !== 'all' && doc.status !== statusFilter) return false;
        if (schemaFilter !== 'all' && doc.schemaCode !== schemaFilter) return false;
        if (queryLower) {
          const matchesSearch =
            doc.title.toLowerCase().includes(queryLower) ||
            doc.projectName.toLowerCase().includes(queryLower) ||
            doc.organizationName.toLowerCase().includes(queryLower) ||
            (doc.author || '').toLowerCase().includes(queryLower);
          if (!matchesSearch) return false;
        }
      }

      return true;
    });

    if (!fromMockFallback && sortBy === '-updatedAt') {
      return filtered;
    }

    return [...filtered].sort((a, b) => {
      if (sortBy === 'title') {
        return a.title.localeCompare(b.title, 'ru');
      }
      if (sortBy === 'status') {
        return STATUS_SORT_ORDER[a.status] - STATUS_SORT_ORDER[b.status];
      }
      return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
    });
  }, [
    documents,
    fromMockFallback,
    isProjectView,
    numericProjectId,
    organizationFilter,
    authorFilter,
    statusFilter,
    schemaFilter,
    searchQuery,
    sortBy,
  ]);

  const handleToggleArchive = async (doc: DocumentItem) => {
    setOpenMenuDocId(null);
    setActionError(null);

    const res =
      doc.status === 'Archived'
        ? await unarchiveDocument(doc.id)
        : await archiveDocument(doc.id);

    if (!res.ok) {
      setActionError(res.error);
      return;
    }

    const nextStatus: DocumentStatus = doc.status === 'Archived' ? 'Draft' : 'Archived';
    setDocuments((prev) =>
      prev.map((item) =>
        item.id === doc.id ? { ...item, status: nextStatus } : item
      )
    );
  };

  const pageTitle = isProjectView ? 'Документы проекта' : 'Все документы';
  const columnCount = isProjectView ? 6 : 7;

  useEffect(() => {
    document.title = `${pageTitle} | Memorandum`;
  }, [pageTitle]);

  return (
    <div className={styles.container}>
      {isProjectView && (
        <nav aria-label="Навигационная цепочка" className={styles.breadcrumb}>
          <Link to="/projects" className={styles.breadcrumbLink}>
            Проекты
          </Link>
          <span aria-hidden="true">/</span>
          <span className={styles.breadcrumbCurrent}>{projectTitle}</span>
        </nav>
      )}

      <div className={styles.headerRow}>
        <h1 id={titleId} className={styles.title}>
          {pageTitle}
        </h1>

        {isProjectView && (
          <div className={styles.headerActions}>
            <Link
              to={`/projects/${numericProjectId}/documents/import`}
              state={{ projectName: projectTitle }}
              className={styles.secondaryPillButton}
            >
              Импорт XML
            </Link>
            <Link
              to={`/projects/${numericProjectId}/documents/new`}
              state={{ projectName: projectTitle }}
              className={styles.primaryPillButton}
            >
              Создать записку
            </Link>
          </div>
        )}
      </div>

      {/* Панель поиска и фильтров */}
      <search className={styles.toolbar} aria-label="Поиск и фильтрация документов">
        <div className={styles.searchWrapper}>
          <label htmlFor={searchInputId} className={styles.visuallyHidden}>
            Поиск документов
          </label>
          <svg
            className={styles.searchIcon}
            viewBox="0 0 20 20"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <circle cx="9" cy="9" r="6" />
            <path d="M13.5 13.5L18 18" />
          </svg>
          <input
            id={searchInputId}
            type="text"
            className={styles.searchInput}
            placeholder="Поиск"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            autoCorrect="off"
            autoCapitalize="none"
            spellCheck={false}
          />
        </div>

        {!isProjectView && (
          <select
            className={styles.select}
            value={organizationFilter}
            onChange={(e) => setOrganizationFilter(e.target.value)}
            aria-label="Организация"
          >
            <option value="all">Организация</option>
            {organizationOptions.map((org) => (
              <option key={org} value={org}>
                {org}
              </option>
            ))}
          </select>
        )}

        <select
          className={styles.select}
          value={authorFilter}
          onChange={(e) => setAuthorFilter(e.target.value)}
          aria-label="Автор"
        >
          <option value="all">Автор</option>
          {authorOptions.map((author) => (
            <option key={author} value={author}>
              {author}
            </option>
          ))}
        </select>

        <select
          className={styles.select}
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          aria-label="Статус"
        >
          <option value="all">Статус</option>
          <option value="Draft">Черновик</option>
          <option value="Verified">Проверен</option>
          <option value="HasErrors">С ошибками</option>
          <option value="Archived">Архивирован</option>
        </select>

        <select
          className={styles.select}
          value={schemaFilter}
          onChange={(e) => setSchemaFilter(e.target.value)}
          aria-label="Схема XML"
        >
          <option value="all">Схема XML</option>
          {schemaOptions.map((code) => (
            <option key={code} value={code}>
              {code}
            </option>
          ))}
        </select>
      </search>

      {/* Вторая строка: сортировка справа */}
      <div className={styles.sortRow}>
        <label htmlFor={sortSelectId} className={styles.sortLabel}>
          Сортировка:
        </label>
        <select
          id={sortSelectId}
          className={`${styles.select} ${styles.sortSelect}`}
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value)}
        >
          <option value="-updatedAt">По последнему изменению</option>
          <option value="title">По названию (А–Я)</option>
          <option value="status">По статусу</option>
        </select>
      </div>

      {actionError && (
        <div role="alert" className={styles.actionError}>
          {actionError}
        </div>
      )}

      {/* Таблица документов */}
      <div
        className={styles.tableWrapper}
        tabIndex={0}
        role="region"
        aria-labelledby={titleId}
      >
        <table className={styles.table}>
          <thead>
            <tr className={styles.tableHeadRow}>
              {!isProjectView && (
                <th scope="col" className={styles.th}>
                  Проект
                </th>
              )}
              <th scope="col" className={styles.th}>
                Документ
              </th>
              {!isProjectView && (
                <th scope="col" className={styles.th}>
                  Организация
                </th>
              )}
              <th scope="col" className={styles.th}>
                Автор
              </th>
              {isProjectView && (
                <th scope="col" className={styles.th}>
                  Изменён
                </th>
              )}
              <th scope="col" className={styles.th}>
                Статус
              </th>
              <th scope="col" className={styles.th}>
                Схема
              </th>
              <th scope="col" className={styles.thActions}>
                <span className={styles.visuallyHidden}>Действия</span>
              </th>
            </tr>
          </thead>

          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columnCount} className={styles.emptyState}>
                  Загрузка документов...
                </td>
              </tr>
            ) : visibleDocuments.length === 0 ? (
              <tr>
                <td colSpan={columnCount} className={styles.emptyState}>
                  {errorMessage || 'Документы не найдены.'}
                </td>
              </tr>
            ) : (
              visibleDocuments.map((doc) => {
                const isMenuOpen = openMenuDocId === doc.id;

                return (
                  <tr key={doc.id} className={styles.tableBodyRow}>
                    {!isProjectView && (
                      <td className={styles.td}>
                        <Link
                          to={`/projects/${doc.projectId}/documents`}
                          state={{ projectName: doc.projectName }}
                          className={styles.primaryLink}
                        >
                          {doc.projectName}
                        </Link>
                      </td>
                    )}

                    <td className={styles.td}>
                      <span
                        className={
                          isProjectView ? styles.primaryCellText : styles.docTitleText
                        }
                      >
                        {doc.title}
                      </span>
                    </td>

                    {!isProjectView && (
                      <td className={styles.td}>
                        <span className={styles.mutedText}>{doc.organizationName}</span>
                      </td>
                    )}

                    <td className={styles.td}>
                      <span className={styles.mutedText}>
                        {doc.author || 'Не указан'}
                      </span>
                    </td>

                    {isProjectView && (
                      <td className={styles.td}>
                        <span className={styles.dateText}>
                          {formatDocumentDate(doc.updatedAt)}
                        </span>
                      </td>
                    )}

                    <td className={styles.td}>
                      <span className={STATUS_STYLE_CLASS[doc.status]}>
                        {STATUS_LABELS[doc.status]}
                      </span>
                    </td>

                    <td className={styles.td}>
                      <span className={styles.schemaText}>{doc.schemaCode}</span>
                    </td>

                    <td
                      className={styles.tdActions}
                      ref={isMenuOpen ? menuAnchorRef : undefined}
                    >
                      <button
                        type="button"
                        className={styles.moreButton}
                        aria-label={`Действия для документа ${doc.title}`}
                        aria-expanded={isMenuOpen}
                        aria-haspopup="menu"
                        onClick={() =>
                          setOpenMenuDocId(isMenuOpen ? null : doc.id)
                        }
                      >
                        <svg
                          width="16"
                          height="4"
                          viewBox="0 0 16 4"
                          fill="currentColor"
                          aria-hidden="true"
                        >
                          <circle cx="2" cy="2" r="1.5" />
                          <circle cx="8" cy="2" r="1.5" />
                          <circle cx="14" cy="2" r="1.5" />
                        </svg>
                      </button>

                      {isMenuOpen && (
                        <div className={styles.rowMenu} role="menu">
                          {!isProjectView && (
                            <button
                              type="button"
                              role="menuitem"
                              className={styles.rowMenuItem}
                              onClick={() => {
                                setOpenMenuDocId(null);
                                navigate(`/projects/${doc.projectId}/documents`, {
                                  state: { projectName: doc.projectName },
                                });
                              }}
                            >
                              Документы проекта
                            </button>
                          )}

                          {doc.status === 'Verified' && (
                            <a
                              role="menuitem"
                              href={`/api/v1/documents/${doc.id}/xml/download`}
                              className={styles.rowMenuItem}
                              onClick={() => setOpenMenuDocId(null)}
                            >
                              Скачать XML
                            </a>
                          )}

                          <button
                            type="button"
                            role="menuitem"
                            className={styles.rowMenuItem}
                            onClick={() => handleToggleArchive(doc)}
                          >
                            {doc.status === 'Archived' ? 'Восстановить' : 'Архивировать'}
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default DocumentsPage;
