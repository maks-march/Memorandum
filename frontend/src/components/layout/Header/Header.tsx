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
          <span className={styles.bracket}>&lt;</span>
          <span className={styles.logoText}>Memorandum</span>
          <span className={styles.bracket}>&gt;</span>
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
