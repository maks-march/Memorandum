// frontend/src/pages/NewDocumentPage/NewDocumentPage.tsx
import React, { useEffect, useId, useRef, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  createDocument,
  getSchemaVersions,
  type SchemaVersionItem,
} from '@/api';
import { useProjectTitle } from '@/pages/DocumentsPage/documentHelpers';
import styles from './NewDocumentPage.module.css';

type LocationState = {
  projectName?: string;
} | null;

/**
 * Страница «Создание пояснительной записки» (строго по макету Figma «Документы - Создание записки»):
 * - Хлебные крошки: «Проекты / <Проект>»
 * - Заголовок: «Создание пояснительной записки»
 * - Поля: Название документа*, Версия XML-схемы (выбор из активных версий бэкенда)
 * - Кнопки: «Отмена» и «Создать записку»
 */
export const NewDocumentPage: React.FC = () => {
  const { projectId: projectIdParam } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  const numericProjectId = Number(projectIdParam);
  const locationState = location.state as LocationState;
  const projectTitle = useProjectTitle(
    !Number.isNaN(numericProjectId) ? numericProjectId : undefined,
    locationState?.projectName
  );

  const [title, setTitle] = useState('Пояснительная записка');
  const [schemas, setSchemas] = useState<SchemaVersionItem[]>([]);
  const [schemasLoading, setSchemasLoading] = useState(true);
  const [schemaCode, setSchemaCode] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const titleInputId = useId();
  const errorId = useId();
  const titleInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    document.title = 'Создание пояснительной записки | Memorandum';
  }, []);

  useEffect(() => {
    let isMounted = true;
    getSchemaVersions().then((res) => {
      if (!isMounted) return;
      setSchemasLoading(false);

      if (res.ok) {
        setSchemas(res.data);
        const defaultSchema = res.data.find((s) => s.isDefault) ?? res.data[0];
        if (defaultSchema) {
          setSchemaCode(defaultSchema.code);
        }
      } else {
        setError(res.error);
      }
    });

    return () => {
      isMounted = false;
    };
  }, []);

  const projectDocsUrl = `/projects/${numericProjectId}/documents`;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (Number.isNaN(numericProjectId)) {
      setError('Некорректный идентификатор проекта');
      return;
    }

    if (!title.trim()) {
      setError('Введите название документа');
      titleInputRef.current?.focus();
      return;
    }

    if (!schemaCode) {
      setError('Выберите версию XML-схемы');
      return;
    }

    setSubmitting(true);
    const result = await createDocument(numericProjectId, {
      title: title.trim(),
      schemaCode,
    });
    setSubmitting(false);

    if (result.ok) {
      navigate(projectDocsUrl, { state: { projectName: projectTitle } });
    } else {
      setError(result.error || 'Не удалось создать пояснительную записку');
    }
  };

  return (
    <div className={styles.container}>
      <nav aria-label="Навигационная цепочка" className={styles.breadcrumb}>
        <Link to="/projects" className={styles.breadcrumbLink}>
          Проекты
        </Link>
        <span aria-hidden="true">/</span>
        <Link
          to={projectDocsUrl}
          state={{ projectName: projectTitle }}
          className={styles.breadcrumbLink}
        >
          {projectTitle}
        </Link>
      </nav>

      <h1 className={styles.title}>Создание пояснительной записки</h1>

      <div className={styles.card}>
        <form onSubmit={handleSubmit} className={styles.form} noValidate>
          {error && (
            <div id={errorId} role="alert" className={styles.errorMessage}>
              {error}
            </div>
          )}

          <div className={styles.field}>
            <label className={styles.label} htmlFor={titleInputId}>
              Название документа<span aria-hidden="true">*</span>
            </label>
            <input
              ref={titleInputRef}
              id={titleInputId}
              name="title"
              type="text"
              required
              aria-invalid={Boolean(error && !title.trim())}
              aria-describedby={error ? errorId : undefined}
              className={styles.input}
              placeholder="Пояснительная записка"
              value={title}
              onChange={(e) => {
                setTitle(e.target.value);
                if (error) setError(null);
              }}
            />
          </div>

          <fieldset className={styles.fieldset}>
            <legend className={styles.legend}>Версия XML-схемы</legend>

            {schemasLoading ? (
              <div className={styles.schemaLoading}>Загрузка доступных версий схем...</div>
            ) : schemas.length === 0 ? (
              <div className={styles.schemaLoading}>Нет доступных версий XML-схем</div>
            ) : (
              <div className={styles.radioList}>
                {schemas.map((schema) => {
                  const isSelected = schemaCode === schema.code;
                  const radioId = `schema-option-${schema.code}`;
                  const labelText = schema.isDefault
                    ? `${schema.code} - актуальная`
                    : schema.code;

                  return (
                    <label
                      key={schema.code}
                      htmlFor={radioId}
                      className={isSelected ? styles.radioCardActive : styles.radioCard}
                    >
                      <input
                        id={radioId}
                        type="radio"
                        name="schemaCode"
                        value={schema.code}
                        checked={isSelected}
                        onChange={() => setSchemaCode(schema.code)}
                        className={styles.radioInput}
                      />
                      <span className={styles.radioText}>{labelText}</span>
                    </label>
                  );
                })}
              </div>
            )}
          </fieldset>

          <div className={styles.actions}>
            <Link
              to={projectDocsUrl}
              state={{ projectName: projectTitle }}
              className={styles.cancelButton}
            >
              Отмена
            </Link>
            <button
              type="submit"
              className={styles.submitButton}
              disabled={submitting}
            >
              {submitting ? 'Создание...' : 'Создать записку'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default NewDocumentPage;
