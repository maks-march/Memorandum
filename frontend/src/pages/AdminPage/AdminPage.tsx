// frontend/src/pages/AdminPage/AdminPage.tsx
import React, { useEffect, useState } from 'react';
import { getCurrentUser, type UserProfile } from '@/api';
import styles from './AdminPage.module.css';

export const AdminPage: React.FC = () => {
  const [user, setUser] = useState<UserProfile | null>(null);

  useEffect(() => {
    document.title = 'Администрирование | Memorandum';
    getCurrentUser().then((u) => {
      if (u) setUser(u);
    });
  }, []);

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Администрирование</h1>

      <div className={styles.card}>
        <div className={styles.section}>
          <span className={styles.label}>Панель управления</span>
          <span className={styles.value}>
            Раздел администрирования системы и управления пользователями
          </span>
        </div>

        <div className={styles.section}>
          <span className={styles.label}>Текущий администратор</span>
          <span className={styles.value}>
            {user?.fullName || 'Пользователь'} ({user?.email || '—'})
          </span>
        </div>
      </div>
    </div>
  );
};

export default AdminPage;
