import { useEffect, useState } from 'react';
import { getProjectById, type DocumentStatus } from '@/api';

export const STATUS_LABELS: Record<DocumentStatus, string> = {
  Draft: 'Черновик',
  Verified: 'Проверен',
  HasErrors: 'С ошибками',
  Archived: 'Архивирован',
};

export const STATUS_SORT_ORDER: Record<DocumentStatus, number> = {
  HasErrors: 0,
  Draft: 1,
  Verified: 2,
  Archived: 3,
};

/**
 * Форматирование даты изменения документа по макету Figma:
 * сегодня -> «Сегодня, HH:MM», в остальные дни -> «DD.MM.YYYY».
 */
export function formatDocumentDate(isoDate: string): string {
  const date = new Date(isoDate);
  if (isNaN(date.getTime())) return '—';

  const now = new Date();
  const isSameDay =
    date.getFullYear() === now.getFullYear() &&
    date.getMonth() === now.getMonth() &&
    date.getDate() === now.getDate();

  if (isSameDay) {
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `Сегодня, ${hours}:${minutes}`;
  }

  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  return `${day}.${month}.${date.getFullYear()}`;
}

/**
 * Загрузка названия проекта для хлебных крошек («Проекты / Название»)
 * с использованием уже переданного имени или GET /api/v1/projects/{id}.
 */
export function useProjectTitle(projectId: number | undefined, initialName?: string): string {
  const [projectTitle, setProjectTitle] = useState<string>(
    initialName || (projectId ? `Проект #${projectId}` : '')
  );

  useEffect(() => {
    if (initialName) {
      setProjectTitle(initialName);
    }
  }, [initialName]);

  useEffect(() => {
    if (!projectId || Number.isNaN(projectId)) return;

    let isMounted = true;
    getProjectById(projectId).then((res) => {
      if (isMounted && res.ok && res.data.name) {
        setProjectTitle(res.data.name);
      }
    });

    return () => {
      isMounted = false;
    };
  }, [projectId]);

  return projectTitle;
}
