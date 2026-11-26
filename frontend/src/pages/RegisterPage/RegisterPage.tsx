import React, { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { registerWithInvite, registerWithOrg } from '@/api';
import styles from './RegisterPage.module.css';

type RegisterMode = 'create-org' | 'invite';

/**
 * Экраны «Регистрация с новой организацией» (node-id=5-292)
 * и «Регистрация с существующей организацией» (node-id=5-591).
 */
export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const [mode, setMode] = useState<RegisterMode>('create-org');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [orgName, setOrgName] = useState('');
  const [inviteCode, setInviteCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleModeChange = (nextMode: RegisterMode) => {
    setMode(nextMode);
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setError('Укажите электронную почту');
      return;
    }
    if (password.length < 8) {
      setError('Пароль должен быть не короче 8 символов');
      return;
    }
    if (password !== confirmPassword) {
      setError('Пароли не совпадают');
      return;
    }

    // Временное значение ФИО из email до уточнения контракта с бекендом
    const fallbackFullName = trimmedEmail.split('@')[0] || 'Пользователь';

    if (mode === 'create-org') {
      const trimmedOrg = orgName.trim();
      if (!trimmedOrg) {
        setError('Укажите название организации');
        return;
      }

      setIsSubmitting(true);
      const result = await registerWithOrg({
        email: trimmedEmail,
        password,
        fullName: fallbackFullName,
        orgName: trimmedOrg,
      });
      setIsSubmitting(false);

      if (!result.ok) {
        setError(result.error);
        return;
      }
    } else {
      const trimmedCode = inviteCode.trim();
      if (!trimmedCode) {
        setError('Введите код приглашения');
        return;
      }

      setIsSubmitting(true);
      const result = await registerWithInvite({
        code: trimmedCode,
        email: trimmedEmail,
        password,
        fullName: fallbackFullName,
      });
      setIsSubmitting(false);

      if (!result.ok) {
        setError(result.error);
        return;
      }
    }

    navigate('/login');
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

      <h1 className={styles.title}>Создать учётную запись</h1>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <div className={styles.field}>
          <label className={styles.label} htmlFor="reg-email">
            Электронная почта
          </label>
          <input
            id="reg-email"
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
          <label className={styles.label} htmlFor="reg-password">
            Пароль
          </label>
          <input
            id="reg-password"
            type="password"
            autoComplete="new-password"
            className={styles.input}
            placeholder="••••••••"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div className={styles.field}>
          <label className={styles.label} htmlFor="reg-confirm-password">
            Подтвердите пароль
          </label>
          <input
            id="reg-confirm-password"
            type="password"
            autoComplete="new-password"
            className={styles.input}
            placeholder="••••••••"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div className={styles.subTabs} role="tablist" aria-label="Способ регистрации">
          <button
            type="button"
            role="tab"
            aria-selected={mode === 'create-org'}
            className={
              mode === 'create-org'
                ? `${styles.subTab} ${styles.subTabActive}`
                : styles.subTab
            }
            onClick={() => handleModeChange('create-org')}
            disabled={isSubmitting}
          >
            Создать организацию
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={mode === 'invite'}
            className={
              mode === 'invite'
                ? `${styles.subTab} ${styles.subTabActive}`
                : styles.subTab
            }
            onClick={() => handleModeChange('invite')}
            disabled={isSubmitting}
          >
            Есть приглашение
          </button>
        </div>

        {mode === 'create-org' ? (
          <div className={styles.field}>
            <label className={styles.label} htmlFor="reg-org-name">
              Название организации
            </label>
            <input
              id="reg-org-name"
              type="text"
              autoComplete="organization"
              className={styles.input}
              placeholder="ООО «Проект»"
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
        ) : (
          <div className={styles.field}>
            <label className={styles.label} htmlFor="reg-invite-code">
              Введите код приглашения
            </label>
            <input
              id="reg-invite-code"
              type="text"
              autoComplete="off"
              className={styles.input}
              placeholder="A1B2C3"
              value={inviteCode}
              onChange={(e) => setInviteCode(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
        )}

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
          {isSubmitting ? 'Регистрация...' : 'Зарегистрироваться'}
        </button>
      </form>
    </section>
  );
};

export default RegisterPage;
