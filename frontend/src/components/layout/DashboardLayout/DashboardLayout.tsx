// frontend/src/components/layout/DashboardLayout/DashboardLayout.tsx
import React, { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { getCurrentUser, logoutUser, type UserProfile } from '@/api';
import styles from './DashboardLayout.module.css';

/**
 * Основной макет приложения с левым сайдбаром (1440px по Figma):
 * - Сайдбар слева: логотип `< Memorandum >`, меню «Проекты», «Документы», «Организация», «Администрирование»
 * - Декоративный шеврон в фоне сайдбара
 * - Профиль пользователя в подвале
 * - Область контента справа
 */
export const DashboardLayout: React.FC = () => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const isDocumentsRoute =
    pathname.startsWith('/documents') || /^\/projects\/[^/]+\/documents/.test(pathname);
  const isProjectsRoute = pathname.startsWith('/projects') && !isDocumentsRoute;

  useEffect(() => {
    let isMounted = true;
    getCurrentUser().then((u) => {
      if (isMounted && u) setUser(u);
    });
    return () => {
      isMounted = false;
    };
  }, []);

  const handleLogout = async (e: React.MouseEvent) => {
    e.preventDefault();
    await logoutUser();
    navigate('/login');
  };

  return (
    <div className={styles.layout}>
      {/* Левый сайдбар */}
      <aside className={styles.sidebar}>
        {/* Фоновый геометрический шеврон `<` строго по стилистике бренда */}
        <svg
          className={styles.chevronBg}
          viewBox="0 0 260 540"
          preserveAspectRatio="none"
          aria-hidden="true"
        >
          <polygon
            points="180,0 260,0 260,60 120,270 260,480 260,540 180,540 20,290 20,250"
            fill="#000000"
          />
        </svg>

        <div className={styles.sidebarHeader}>
          <NavLink to="/projects" className={styles.logo}>
            <span className={styles.logoBracket}>&lt;</span>
            <span className={styles.logoText}>Memorandum</span>
            <span className={styles.logoBracket}>&gt;</span>
          </NavLink>
        </div>

        {/* Навигационное меню */}
        <nav className={styles.nav} aria-label="Основное меню">
          <NavLink
            to="/projects"
            className={() =>
              `${styles.navItem} ${isProjectsRoute ? styles.navItemActive : ''}`
            }
          >
            Проекты
          </NavLink>

          <NavLink
            to="/documents"
            className={() =>
              `${styles.navItem} ${isDocumentsRoute ? styles.navItemActive : ''}`
            }
          >
            Документы
          </NavLink>

          <NavLink
            to="/organization"
            className={({ isActive }) =>
              `${styles.navItem} ${isActive ? styles.navItemActive : ''}`
            }
          >
            Организация
          </NavLink>

          <NavLink
            to="/admin"
            className={({ isActive }) =>
              `${styles.navItem} ${isActive ? styles.navItemActive : ''}`
            }
          >
            Администрирование
          </NavLink>
        </nav>

        {/* Подвал сайдбара: ФИО пользователя и кнопка «Выйти» в одну строку */}
        <div className={styles.sidebarFooter}>
          <span className={styles.userName}>{user ? user.fullName : 'Пользователь'}</span>
          <button type="button" onClick={handleLogout} className={styles.logoutButton}>
            Выйти
          </button>
        </div>
      </aside>

      {/* Основная область контента */}
      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  );
};

export default DashboardLayout;