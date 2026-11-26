import React, { useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { loginUser } from '@/api';
import styles from './LoginPage.module.css';

/**
 * Экран «Вход» (строго по фрейму Figma node-id=5-76).
 */
export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    const trimmedEmail = email.trim();
    if (!trimmedEmail || !password) {
      setError('Введите электронную почту и пароль');
      return;
    }

    setIsSubmitting(true);
    const result = await loginUser(trimmedEmail, password);
    setIsSubmitting(false);

    if (!result.ok) {
      setError(result.error);
      return;
    }

    // После успешного входа переходим в рабочее пространство на /projects
    navigate('/projects');
  };

  return (
    <section className={styles.wrapper}>
      <nav className={styles.tabs} aria-label="Переключение режима">
        <NavLink
          to="/login"
          className={({ isActive }) =>
            isActive ? `${styles.tab} ${styles.tabActive}` : styles.tab
          }
        >
          Вход
        </NavLink>
        <NavLink
          to="/register"
          className={({ isActive }) =>
            isActive ? `${styles.tab} ${styles.tabActive}` : styles.tab
          }
        >
          Регистрация
        </NavLink>
      </nav>

      <h1 className={styles.title}>Добро пожаловать</h1>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <div className={styles.field}>
          <label className={styles.label} htmlFor="login-email">
            Электронная почта
          </label>
          <input
            id="login-email"
            type="email"
            autoComplete="email"
            className={styles.input}
            placeholder="name@company.ru"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div className={styles.field}>
          <label className={styles.label} htmlFor="login-password">
            Пароль
          </label>
          <input
            id="login-password"
            type="password"
            autoComplete="current-password"
            className={styles.input}
            placeholder="••••••••"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
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
          {isSubmitting ? 'Вход...' : 'Войти'}
        </button>

        <div className={styles.footerLinkWrap}>
          <Link to="/forgot-password" className={styles.footerLink}>
            Забыли пароль?
          </Link>
        </div>
      </form>
    </section>
  );
};

export default LoginPage;
