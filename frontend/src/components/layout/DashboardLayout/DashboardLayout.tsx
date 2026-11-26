// frontend/src/components/layout/DashboardLayout/DashboardLayout.tsx
import React, { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { getCurrentUser, logoutUser, type UserProfile } from '@/api';
import styles from './DashboardLayout.module.css';

/**
 * Основной макет приложения с левым сайдбаром (1440px по Figma):
 * - Сайдбар слева: логотип `< Memorandum >`, меню «Проекты», «Документы», «Организация», «Администрирование»
 * - Декоративный шеврон `>` в фоне сайдбара строго по макету
 * - Профиль пользователя в подвале
 * - Адаптивная поддержка всех экранов (десктоп, планшет, мобильные)
 */
export const DashboardLayout: React.FC = () => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isMobileNavOpen, setIsMobileNavOpen] = useState(false);
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

  // Закрывать мобильную шторку при переходе по ссылке
  useEffect(() => {
    setIsMobileNavOpen(false);
  }, [pathname]);

  // Закрывать по кнопке Escape
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsMobileNavOpen(false);
    };
    if (isMobileNavOpen) {
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isMobileNavOpen]);

  const handleLogout = async (e: React.MouseEvent) => {
    e.preventDefault();
    await logoutUser();
    navigate('/login');
  };

  return (
    <div className={styles.layout}>
      {/* Мобильная верхняя панель (активна только на экранах <= 900px) */}
      <header className={styles.mobileHeader}>
        <button
          type="button"
          className={styles.hamburgerButton}
          onClick={() => setIsMobileNavOpen(!isMobileNavOpen)}
          aria-label={isMobileNavOpen ? 'Закрыть меню навигации' : 'Открыть меню навигации'}
          aria-expanded={isMobileNavOpen}
        >
          <svg
            width="22"
            height="22"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <line x1="3" y1="6" x2="21" y2="6" />
            <line x1="3" y1="12" x2="21" y2="12" />
            <line x1="3" y1="18" x2="21" y2="18" />
          </svg>
        </button>

        <NavLink
          to="/projects"
          className={styles.mobileLogo}
          onClick={() => setIsMobileNavOpen(false)}
        >
          <svg
            className={styles.logoBracketIcon}
            width="8"
            height="15"
            viewBox="0 0 10 18"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M8 2L2 9L8 16"
              stroke="#1C1C1E"
              strokeWidth="2.75"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
          <span className={styles.logoText}>Memorandum</span>
          <svg
            className={styles.logoBracketIcon}
            width="8"
            height="15"
            viewBox="0 0 10 18"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M2 2L8 9L2 16"
              stroke="#1C1C1E"
              strokeWidth="2.75"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </NavLink>

        <span className={styles.mobileUser}>{user ? user.fullName.split(' ')[0] : ''}</span>
      </header>

      {/* Затемнение фона при открытой мобильной шторке */}
      {isMobileNavOpen && (
        <div
          className={styles.mobileBackdrop}
          onClick={() => setIsMobileNavOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Левый сайдбар (десктопная панель / мобильная шторка) */}
      <aside
        className={`${styles.sidebar} ${isMobileNavOpen ? styles.sidebarOpen : ''}`}
        aria-label="Основная навигация"
      >
        {/* Фоновый геометрический шеврон `>` строго по макету Figma */}
        <svg
          className={styles.chevronBg}
          viewBox="0 0 260 610"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden="true"
        >
          <polygon
            points="0,10 260,270 260,350 0,610 0,430 120,310 0,190"
            fill="#ECECEE"
          />
        </svg>

        <div className={styles.sidebarHeader}>
          <NavLink
            to="/projects"
            className={styles.logo}
            onClick={() => setIsMobileNavOpen(false)}
          >
            <svg
              className={styles.logoBracketIcon}
              width="10"
              height="18"
              viewBox="0 0 10 18"
              fill="none"
              aria-hidden="true"
            >
              <path
                d="M8 2L2 9L8 16"
                stroke="#1C1C1E"
                strokeWidth="2.75"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            <span className={styles.logoText}>Memorandum</span>
            <svg
              className={styles.logoBracketIcon}
              width="10"
              height="18"
              viewBox="0 0 10 18"
              fill="none"
              aria-hidden="true"
            >
              <path
                d="M2 2L8 9L2 16"
                stroke="#1C1C1E"
                strokeWidth="2.75"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </NavLink>

          {/* Кнопка закрытия внутри шторки на мобильных экранах */}
          <button
            type="button"
            className={styles.closeDrawerButton}
            onClick={() => setIsMobileNavOpen(false)}
            aria-label="Закрыть меню"
          >
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        {/* Навигационное меню */}
        <nav className={styles.nav} aria-label="Основное меню">
          <NavLink
            to="/projects"
            className={() =>
              `${styles.navItem} ${isProjectsRoute ? styles.navItemActive : ''}`
            }
            onClick={() => setIsMobileNavOpen(false)}
          >
            Проекты
          </NavLink>

          <NavLink
            to="/documents"
            className={() =>
              `${styles.navItem} ${isDocumentsRoute ? styles.navItemActive : ''}`
            }
            onClick={() => setIsMobileNavOpen(false)}
          >
            Документы
          </NavLink>

          <NavLink
            to="/organization"
            className={({ isActive }) =>
              `${styles.navItem} ${isActive ? styles.navItemActive : ''}`
            }
            onClick={() => setIsMobileNavOpen(false)}
          >
            Организация
          </NavLink>

          <NavLink
            to="/admin"
            className={({ isActive }) =>
              `${styles.navItem} ${isActive ? styles.navItemActive : ''}`
            }
            onClick={() => setIsMobileNavOpen(false)}
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