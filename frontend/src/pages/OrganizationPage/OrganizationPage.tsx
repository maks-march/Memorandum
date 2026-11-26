// frontend/src/pages/OrganizationPage/OrganizationPage.tsx
import React, { useEffect, useState } from 'react';
import { getCurrentUser, type UserProfile } from '@/api';
import styles from './OrganizationPage.module.css';

export const OrganizationPage: React.FC = () => {
  const [user, setUser] = useState<UserProfile | null>(null);

  useEffect(() => {
    document.title = 'Организация | Memorandum';
    getCurrentUser().then((u) => {
      if (u) setUser(u);
    });
  }, []);

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Организация</h1>

      <div className={styles.card}>
        <div className={styles.section}>
          <span className={styles.label}>Наименование организации</span>
          <span className={styles.value}>
            {user?.organizationName || 'Организация не указана'}
          </span>
        </div>

        <div className={styles.section}>
          <span className={styles.label}>Текущий пользователь</span>
          <span className={styles.value}>
            {user?.fullName || 'Пользователь'} ({user?.email || '—'})
          </span>
        </div>
      </div>
    </div>
  );
};

export default OrganizationPage;
