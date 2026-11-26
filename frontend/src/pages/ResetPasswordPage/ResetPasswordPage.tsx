import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { confirmPasswordReset } from '@/api';
import styles from './ResetPasswordPage.module.css';

/**
 * Экран «Восстановление пароля - новый пароль» (строго по фрейму Figma node-id=6-813).
 */
export const ResetPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    if (newPassword.length < 8) {
      setError('Пароль должен быть не короче 8 символов');
      return;
    }
    if (newPassword !== confirmPassword) {
      setError('Пароли не совпадают');
      return;
    }

    const token = searchParams.get('token') ?? '';
    setIsSubmitting(true);
    const result = await confirmPasswordReset(token, newPassword);
    setIsSubmitting(false);

    if (!result.ok) {
      setError(result.error);
      return;
    }

    navigate('/login');
  };

  return (
    <section className={styles.wrapper}>
      <h1 className={styles.title}>Восстановление доступа</h1>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <div className={styles.field}>
          <label className={styles.label} htmlFor="reset-password">
            Новый пароль
          </label>
          <input
            id="reset-password"
            type="password"
            autoComplete="new-password"
            className={styles.input}
            placeholder="••••••••"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div className={styles.field}>
          <label className={styles.label} htmlFor="reset-confirm-password">
            Подтвердите пароль
          </label>
          <input
            id="reset-confirm-password"
            type="password"
            autoComplete="new-password"
            className={styles.input}
            placeholder="••••••••"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        {error && (
          <p className={styles.errorText} role="alert">
            {error}
          </p>
        )}

        <button
          type="submit"
          className={styles.submitButton}
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Сохранение...' : 'Подтвердить и вернуться ко входу'}
        </button>
      </form>
    </section>
  );
};

export default ResetPasswordPage;
