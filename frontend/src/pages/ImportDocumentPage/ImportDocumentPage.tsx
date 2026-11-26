// frontend/src/pages/ImportDocumentPage/ImportDocumentPage.tsx
import React, { useRef, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { importDocumentXml, type ImportWarningItem } from '@/api';
import { useProjectTitle } from '@/pages/DocumentsPage/documentHelpers';
import styles from './ImportDocumentPage.module.css';

type LocationState = {
  projectName?: string;
} | null;

type ImportViewState =
  | { status: 'idle' }
  | { status: 'uploading'; fileName: string }
  | {
      status: 'error';
      fileName: string;
      detectedSchema?: string;
      title: string;
      detail?: string;
    }
  | {
      status: 'success';
      fileName: string;
      documentId: number;
      detectedSchema: string;
      warnings: ImportWarningItem[];
    };

/**
 * Извлекает код версии XML-схемы из начала файла только для отображения в ошибке,
 * если сервер вернул 415/422 без поля detectedSchema в ProblemDetails.
 */
async function extractSchemaHintFromXml(file: File): Promise<string | undefined> {
  try {
    const head = await file.slice(0, 4096).text();
    const attrMatch = head.match(
      /(?:SchemaVersion|schemaVersion|ВерсияСхемы|SchemaCode)\s*=\s*["']([^"']{2,20})["']/i
    );
    if (attrMatch?.[1]) return attrMatch[1];

    const tagMatch = head.match(/<SchemaVersion>\s*([^<\s]{2,20})\s*<\/SchemaVersion>/i);
    if (tagMatch?.[1]) return tagMatch[1];
  } catch {
    // Ошибка чтения префикса файла не критична
  }
  return undefined;
}

/**
 * Страница «Импорт XML» (строго по 3 макетам Figma):
 * 1. До импорта: пунктирная зона перетаскивания + кнопка «Выбрать файл»
 * 2. Неподходящая версия / ошибка: карточка с именем файла, красной версией, описанием и кнопкой «Загрузить другой файл»
 * 3. Подходящая версия: карточка с именем файла, зеленой версией, статусом переноса и кнопками «Открыть в редакторе» / «Отмена»
 */
export const ImportDocumentPage: React.FC = () => {
  const { projectId: projectIdParam } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  const numericProjectId = Number(projectIdParam);
  const locationState = location.state as LocationState;
  const projectTitle = useProjectTitle(
    !Number.isNaN(numericProjectId) ? numericProjectId : undefined,
    locationState?.projectName
  );

  const [viewState, setViewState] = useState<ImportViewState>({ status: 'idle' });
  const [isDragOver, setIsDragOver] = useState(false);

  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const dragDepthRef = useRef(0);

  React.useEffect(() => {
    document.title = 'Импорт XML | Memorandum';
  }, []);

  const projectDocsUrl = `/projects/${numericProjectId}/documents`;

  const processFile = async (file: File) => {
    if (Number.isNaN(numericProjectId)) {
      setViewState({
        status: 'error',
        fileName: file.name,
        title: 'Некорректный идентификатор проекта',
      });
      return;
    }

    if (!file.name.toLowerCase().endsWith('.xml')) {
      setViewState({
        status: 'error',
        fileName: file.name,
        title: 'Неподдерживаемый формат файла',
        detail: 'Загрузите файл пояснительной записки в формате XML (.xml).',
      });
      return;
    }

    setViewState({ status: 'uploading', fileName: file.name });

    const res = await importDocumentXml(numericProjectId, file);

    if (res.ok) {
      setViewState({
        status: 'success',
        fileName: file.name,
        documentId: res.data.documentId,
        detectedSchema: res.data.detectedSchema,
        warnings: res.data.warnings,
      });
      return;
    }

    const schemaHint = res.detectedSchema || (await extractSchemaHintFromXml(file));
    const isUnsupportedVersion = res.status === 415;

    setViewState({
      status: 'error',
      fileName: file.name,
      detectedSchema: schemaHint,
      title: isUnsupportedVersion ? 'Версия схемы не поддерживается' : res.error,
      detail:
        res.detail ||
        (isUnsupportedVersion ? 'Загрузите документ версии 01.06 или 01.07.' : undefined),
    });
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (file) {
      void processFile(file);
    }
  };

  const handleDragEnter = (e: React.DragEvent) => {
    e.preventDefault();
    dragDepthRef.current += 1;
    setIsDragOver(true);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    dragDepthRef.current = Math.max(0, dragDepthRef.current - 1);
    if (dragDepthRef.current === 0) {
      setIsDragOver(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    dragDepthRef.current = 0;
    setIsDragOver(false);

    const file = e.dataTransfer.files?.[0];
    if (file) {
      void processFile(file);
    }
  };

  const handleReset = () => {
    setViewState({ status: 'idle' });
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

      <h1 className={styles.title}>Импорт XML</h1>

      <input
        ref={fileInputRef}
        type="file"
        accept=".xml,text/xml,application/xml"
        className={styles.visuallyHidden}
        aria-label="Выберите XML-файл"
        onChange={handleFileChange}
      />

      {(viewState.status === 'idle' || viewState.status === 'uploading') && (
        <div
          className={`${styles.dropzone} ${isDragOver ? styles.dropzoneActive : ''}`}
          onDragEnter={handleDragEnter}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
        >
          <p className={styles.dropzoneText}>
            Выберите XML-файл на компьютере или перетащите в это окно
          </p>
          <button
            type="button"
            className={styles.primaryPillButton}
            disabled={viewState.status === 'uploading'}
            onClick={() => fileInputRef.current?.click()}
          >
            {viewState.status === 'uploading' ? 'Загрузка файла...' : 'Выбрать файл'}
          </button>
        </div>
      )}

      {viewState.status === 'error' && (
        <div className={styles.resultCard} role="alert">
          <div className={styles.resultHeader}>
            <span className={styles.fileName}>{viewState.fileName}</span>
            {viewState.detectedSchema && (
              <span className={styles.versionError}>
                Версия {viewState.detectedSchema}
              </span>
            )}
          </div>

          <p className={styles.errorTitle}>{viewState.title}</p>
          {viewState.detail && (
            <p className={styles.errorDetail}>{viewState.detail}</p>
          )}

          <div className={styles.cardActions}>
            <button
              type="button"
              className={styles.secondaryPillButton}
              onClick={handleReset}
            >
              Загрузить другой файл
            </button>
          </div>
        </div>
      )}

      {viewState.status === 'success' && (
        <div className={styles.resultCard} role="status">
          <div className={styles.resultHeader}>
            <span className={styles.fileName}>{viewState.fileName}</span>
            {viewState.detectedSchema && (
              <span className={styles.versionSuccess}>
                Версия {viewState.detectedSchema}
              </span>
            )}
          </div>

          <p className={styles.successText}>Все данные перенесены в формы</p>

          {viewState.warnings.length > 0 && (
            <ul className={styles.warningsList}>
              {viewState.warnings.map((w, idx) => (
                <li key={`${w.path}-${idx}`}>
                  {w.path}: {w.reason}
                </li>
              ))}
            </ul>
          )}

          <div className={styles.cardActions}>
            <button
              type="button"
              className={styles.primaryPillButton}
              onClick={() =>
                navigate(projectDocsUrl, {
                  state: {
                    projectName: projectTitle,
                    importedDocumentId: viewState.documentId,
                  },
                })
              }
            >
              Открыть в редакторе
            </button>
            <Link
              to={projectDocsUrl}
              state={{ projectName: projectTitle }}
              className={styles.secondaryPillButton}
            >
              Отмена
            </Link>
          </div>
        </div>
      )}
    </div>
  );
};

export default ImportDocumentPage;
