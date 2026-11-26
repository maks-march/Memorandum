// frontend/src/pages/ProjectsPage/ProjectsPage.tsx
import React, { useEffect, useMemo, useState } from 'react';
import { NavLink } from 'react-router-dom';
import { getProjects, type ProjectItem } from '@/api';
import styles from './ProjectsPage.module.css';

/**
 * Страница «Проекты» (строго по макетам Figma: «Проекты», «Поиск», «Фильтрация»):
 * - Таблица проектов организации
 * - Живой поиск по названию/адресу
 * - Фильтрация по виду работ и типу объекта
 * - Кнопка «+ Новый проект»
 * - Работает как с бэкендом (/api/v1/projects), так и с локальным мок-хранилищем
 */
export const ProjectsPage: React.FC = () => {
  const [projects, setProjects] = useState<ProjectItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Фильтры
  const [searchQuery, setSearchQuery] = useState('');
  const [workTypeFilter, setWorkTypeFilter] = useState('all');
  const [objectFilter, setObjectFilter] = useState('all');

  // Запрос проектов через API бэкенда с параметром поиска ?q=...
  useEffect(() => {
    let isMounted = true;
    const timeoutId = setTimeout(() => {
      getProjects({ q: searchQuery, workType: workTypeFilter }).then(({ items, error }) => {
        if (isMounted) {
          setProjects(items);
          setErrorMessage(error || null);
          setLoading(false);
        }
      });
    }, 120);

    return () => {
      isMounted = false;
      clearTimeout(timeoutId);
    };
  }, [searchQuery, workTypeFilter]);

  // Фильтрация по типам объектов
  const filteredProjects = useMemo(() => {
    if (objectFilter === 'all') return projects;
    return projects.filter((p) => {
      if (objectFilter === 'residential') {
        return p.objectName.toLowerCase().includes('жилой') || p.name.toLowerCase().includes('жилой');
      } else if (objectFilter === 'public') {
        return p.objectName.toLowerCase().includes('школ') || p.name.toLowerCase().includes('школ');
      } else if (objectFilter === 'industrial') {
        return p.objectName.toLowerCase().includes('склад') || p.name.toLowerCase().includes('склад');
      }
      return true;
    });
  }, [projects, objectFilter]);

  return (
    <div className={styles.container}>
      {/* Заголовок и кнопка создания */}
      <div className={styles.headerRow}>
        <h1 className={styles.title}>Проекты</h1>
        <NavLink to="/projects/new" className={styles.createButton}>
          + Новый проект
        </NavLink>
      </div>

      {/* Панель фильтров: поиск + выпадающие списки (макет Figma) */}
      <div className={styles.toolbar}>
        {/* Поиск */}
        <div className={styles.searchWrapper}>
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
            type="text"
            className={styles.searchInput}
            placeholder="Поиск..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>

        {/* Выпадающий список «Вид работ» */}
        <select
          className={styles.select}
          value={workTypeFilter}
          onChange={(e) => setWorkTypeFilter(e.target.value)}
          aria-label="Фильтр по виду работ"
        >
          <option value="all">Все виды работ</option>
          <option value="Строительство">Строительство</option>
          <option value="Реконструкция">Реконструкция</option>
          <option value="Капитальный ремонт">Капитальный ремонт</option>
          <option value="Снос">Снос</option>
          <option value="Иные виды работ">Иные виды работ</option>
        </select>

        {/* Выпадающий список «Все объекты» */}
        <select
          className={styles.select}
          value={objectFilter}
          onChange={(e) => setObjectFilter(e.target.value)}
          aria-label="Фильтр по типам объектов"
        >
          <option value="all">Все объекты</option>
          <option value="residential">Жилые дома</option>
          <option value="public">Общественные здания</option>
          <option value="industrial">Производственные / склады</option>
        </select>
      </div>

      {/* Таблица проектов */}
      <div className={styles.tableContainer}>
        <table className={styles.table}>
          <thead>
            <tr className={styles.tableHeadRow}>
              <th className={styles.th}>Проект</th>
              <th className={styles.th}>Адрес</th>
              <th className={styles.th}>Вид работ</th>
              <th className={styles.th}>Ответственный</th>
              <th className={styles.th}>Создан</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={5} className={styles.emptyState}>
                  Загрузка проектов...
                </td>
              </tr>
            ) : filteredProjects.length === 0 ? (
              <tr>
                <td colSpan={5} className={styles.emptyState}>
                  {errorMessage || 'Проекты не найдены. Попробуйте изменить параметры поиска или фильтрации.'}
                </td>
              </tr>
            ) : (
              filteredProjects.map((project) => (
                <tr key={project.id} className={styles.tableBodyRow}>
                  <td className={styles.td}>
                    <span className={styles.projectName}>{project.name}</span>
                  </td>
                  <td className={styles.td}>
                    <span className={styles.projectAddress}>{project.addressText}</span>
                  </td>
                  <td className={styles.td}>
                    <span className={styles.projectWorkType}>{project.workType}</span>
                  </td>
                  <td className={styles.td}>
                    <span className={styles.projectResponsible}>{project.responsibleName}</span>
                  </td>
                  <td className={styles.td}>
                    <span className={styles.projectDate}>{project.createdAt}</span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default ProjectsPage;