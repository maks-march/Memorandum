// frontend/src/pages/HomePage/HomePage.tsx
import React from 'react';
import { NavLink } from 'react-router-dom';
import styles from './HomePage.module.css';

/**
 * Фрейм «Главная страница» (1440 × 941 px) в 100% точном соответствии с Figma:
 * - Заголовок: Inter Bold 64px
 * - Подзаголовок: Inter Regular 18px (цвет #6E6E73)
 * - Кнопки: «Начать работу» (#0071E3) и «Войти» (#EAF3FF)
 * - 3 карточки в ряд:
 *   1. «Формы вместо XML»
 *   2. «Проверка по XSD»
 *   3. «Версии и история»
 * - Фоновый фирменный геометрический паттерн шевронов (#F5F5F7)
 */
export const HomePage: React.FC = () => {
  return (
    <div className={styles.page}>
      {/* Фирменный геометрический фон с шевронами < > из Figma */}
      <div className={styles.backgroundPattern}>
        {/* Левый шеврон < */}
        <svg
          className={styles.chevronSvg}
          viewBox="0 0 500 800"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          preserveAspectRatio="none"
        >
          <path
            d="M 280 0 L 80 400 L 280 800 L 480 800 L 280 400 L 480 0 Z"
            fill="currentColor"
            opacity="0.8"
          />
        </svg>

        {/* Правый шеврон > */}
        <svg
          className={styles.chevronSvg}
          viewBox="0 0 500 800"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          preserveAspectRatio="none"
        >
          <path
            d="M 220 0 L 420 400 L 220 800 L 20 800 L 220 400 L 20 0 Z"
            fill="currentColor"
            opacity="0.8"
          />
        </svg>
      </div>

      {/* Центральный контент Hero */}
      <div className={styles.heroContainer}>
        {/* Заголовок H1 (64px в Figma) */}
        <h1 className={styles.title}>
          Пояснительная записка<br />в формате XML
        </h1>

        {/* Подзаголовок (18px в Figma) */}
        <p className={styles.subtitle}>
          Веб-сервис для проектных организаций и специалистов<br className={styles.breakSm} /> строительной экспертизы
        </p>

        {/* Кнопки действий */}
        <div className={styles.buttonGroup}>
          <NavLink to="/register" className={styles.primaryButton}>
            Начать работу
          </NavLink>
          <NavLink to="/login" className={styles.secondaryButton}>
            Войти
          </NavLink>
        </div>
      </div>

      {/* 3 карточки преимуществ внизу экрана */}
      <div className={styles.cardsContainer}>
        <div className={styles.cardsGrid}>
          {/* Карточка 1 */}
          <div className={styles.card}>
            <h3 className={styles.cardTitle}>
              Формы вместо XML
            </h3>
            <p className={styles.cardText}>
              Заполняйте сведения об объекте, участниках и ТЭП в привычных формах.
            </p>
          </div>

          {/* Карточка 2 */}
          <div className={styles.card}>
            <h3 className={styles.cardTitle}>
              Проверка по XSD
            </h3>
            <p className={styles.cardText}>
              Обязательные поля и структура проверяются по выбранной версии схемы.
            </p>
          </div>

          {/* Карточка 3 */}
          <div className={styles.card}>
            <h3 className={styles.cardTitle}>
              Версии и история
            </h3>
            <p className={styles.cardText}>
              Храните редакции документов и возвращайтесь к любой из них.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default HomePage;
