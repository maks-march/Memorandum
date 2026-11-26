// frontend/src/components/layout/Header/Header.tsx
import React from 'react';
import { NavLink } from 'react-router-dom';
import styles from './Header.module.css';

/**
 * Хедер главной страницы (Вариант Б по макету Figma «Элементы»):
 * - Слева: логотип `< Memorandum >`
 * - Справа: ссылка `Войти` + кнопка `Регистрация`
 */
export const Header: React.FC = () => {
  return (
    <header className={styles.header}>
      <div className={styles.container}>
        {/* Логотип: < Memorandum > */}
        <NavLink to="/" className={styles.logo}>
          <svg
            className={styles.bracketIcon}
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
            className={styles.bracketIcon}
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

        {/* Действия справа: Войти + Регистрация (Вариант Б) */}
        <div className={styles.actions}>
          <NavLink to="/login" className={styles.loginLink}>
            Войти
          </NavLink>
          <NavLink to="/register" className={styles.registerButton}>
            Регистрация
          </NavLink>
        </div>
      </div>
    </header>
  );
};

export default Header;
