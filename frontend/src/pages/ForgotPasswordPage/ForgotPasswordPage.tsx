import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { requestPasswordReset } from '@/api';
import styles from './ForgotPasswordPage.module.css';

/**
 * Экран «Восстановление пароля - код на почту» (строго по фрейму Figma node-id=5-655).
 */
export const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setError('Укажите электронную почту');
      return;
    }

    setIsSubmitting(true);
    const result = await requestPasswordReset(trimmedEmail);
    setIsSubmitting(false);

    if (!result.ok) {
      setError(result.error);
      return;
    }

    setSent(true);
  };

  return (
    <section className={styles.wrapper}>
      <h1 className={styles.title}>Восстановление доступа</h1>
      <p className={styles.subtitle}>
        Укажите почту, мы отправим ссылку для создания нового пароля.
      </p>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <div className={styles.field}>
          <label className={styles.label} htmlFor="forgot-email">
            Электронная почта
          </label>
          <input
            id="forgot-email"
            type="email"
            autoComplete="email"
            className={styles.input}
            placeholder="name@company.ru"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        {error && (
          <p className={styles.errorText} role="alert">
            {error}
          </p>
        )}

        {sent && (
          <p className={styles.successText} role="status">
            Если аккаунт с таким адресом существует, ссылка отправлена на почту.
          </p>
        )}

        <button
          type="submit"
          className={styles.submitButton}
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Отправка...' : 'Отправить ссылку'}
        </button>

        <div className={styles.footerLinkWrap}>
          <Link to="/login" className={styles.footerLink}>
            Вернуться ко входу
          </Link>
        </div>
      </form>
    </section>
  );
};

export default ForgotPasswordPage;
