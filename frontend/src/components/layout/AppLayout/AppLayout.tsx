// frontend/src/components/layout/AppLayout/AppLayout.tsx
import React from 'react';
import { Outlet } from 'react-router-dom';
import { Header } from '../Header/Header';
import styles from './AppLayout.module.css';

/**
 * Базовый макет для главной страницы.
 */
export const AppLayout: React.FC = () => {
  return (
    <div className={styles.layout}>
      <Header />
      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  );
};

export default AppLayout;
