import React from 'react';
import { Link, Outlet } from 'react-router-dom';
import styles from './AuthLayout.module.css';

/**
 * Общий split-макет для всех страниц авторизации (1440×1024 по Figma):
 * - Слева (45%): синяя панель #0071E3 с геометрическим шевроном, логотипом и слоганом
 * - Справа (55%): белая область с центрированной формой
 */
export const AuthLayout: React.FC = () => {
  return (
    <div className={styles.page}>
      <aside className={styles.brandPanel}>
        {/* Геометрический фоновый шеврон `<` строго по макету Figma */}
        <svg
          className={styles.chevronBg}
          viewBox="0 0 650 1024"
          preserveAspectRatio="xMidYMid slice"
          aria-hidden="true"
        >
          <polygon
            points="428,0 650,0 650,145 304,512 650,879 650,1024 428,1024 0,564 0,460"
            fill="rgba(0, 56, 130, 0.16)"
          />
        </svg>

        <div className={styles.brandContent}>
          <Link to="/" className={styles.logo}>
            <span className={styles.logoBracket}>&lt;</span>
            <span className={styles.logoText}>Memorandum</span>
            <span className={styles.logoBracket}>&gt;</span>
          </Link>

          <p className={styles.tagline}>
            Готовьте
            <br />
            пояснительную
            <br />
            записку спокойно
          </p>
        </div>
      </aside>

      <main className={styles.formPanel}>
        <div className={styles.formContainer}>
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default AuthLayout;
