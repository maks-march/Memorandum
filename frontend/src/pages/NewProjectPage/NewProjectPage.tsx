// frontend/src/pages/NewProjectPage/NewProjectPage.tsx
import React, { useEffect, useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  createProject,
  getCurrentUser,
  type UserProfile,
} from '@/api';
import styles from './NewProjectPage.module.css';

/**
 * Страница «Новый проект» (строго по макету Figma «Проекты - создание»):
 * - Форма создания объекта капитального строительства
 * - Поля: Наименование проекта*, Организация*, Ответственный пользователь*,
 *   Наименование объекта*, Адрес*, Вид работ*, Дата создания*
 * - Кнопки: «Отмена» и «Создать проект»
 * - Сохранение в ASP.NET Core API (/api/v1/projects)
 */
export const NewProjectPage: React.FC = () => {
  const navigate = useNavigate();
  const [currentUser, setCurrentUser] = useState<UserProfile | null>(null);

  // Состояние полей формы
  const [name, setName] = useState('');
  const [responsibleId, setResponsibleId] = useState<number | null>(null);
  const [objectName, setObjectName] = useState('');
  const [addressText, setAddressText] = useState('');
  const [workType, setWorkType] = useState('Строительство');

  // Форматированная дата создания (сегодня)
  const todayFormatted = (() => {
    const d = new Date();
    return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${d.getFullYear()}`;
  })();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    getCurrentUser().then((u) => {
      if (isMounted && u) {
        setCurrentUser(u);
        setResponsibleId(u.id);
      }
    });
    return () => {
      isMounted = false;
    };
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError('Введите наименование проекта');
      return;
    }
    if (!objectName.trim()) {
      setError('Введите наименование объекта');
      return;
    }
    if (!addressText.trim()) {
      setError('Введите адрес (местоположение) объекта');
      return;
    }

    setLoading(true);

    const result = await createProject({
      name: name.trim(),
      objectName: objectName.trim(),
      addressText: addressText.trim(),
      workType,
      responsibleUserId: responsibleId ?? currentUser?.id ?? null,
      responsibleName: currentUser?.fullName,
      organizationName: currentUser?.organizationName || undefined,
      createdAt: todayFormatted,
    });

    setLoading(false);

    if (result.ok) {
      // Успешно создан на сервере — переходим к списку проектов
      navigate('/projects');
    } else {
      setError(result.error || 'Не удалось сохранить проект');
    }
  };

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Новый проект</h1>

      <div className={styles.card}>
        <form onSubmit={handleSubmit} className={styles.form}>
          {error && <div className={styles.errorMessage}>{error}</div>}

          {/* 1. Наименование проекта * */}
          <div className={styles.field}>
            <label className={styles.label} htmlFor="name">
              Наименование проекта<span className={styles.asterisk}>*</span>
            </label>
            <input
              id="name"
              type="text"
              className={styles.input}
              placeholder="Жилой комплекс № 1"
              value={name}
              onChange={(e) => setName(e.target.value)}
              autoFocus
            />
          </div>

          {/* 2. Ряд из двух колонок: Организация* и Ответственный пользователь* */}
          <div className={styles.row}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="org">
                Организация<span className={styles.asterisk}>*</span>
              </label>
              <input
                id="org"
                type="text"
                className={`${styles.input} ${styles.inputDisabled}`}
                value={currentUser?.organizationName || ''}
                placeholder="Организация не указана"
                disabled
                readOnly
              />
            </div>

            <div className={styles.field}>
              <label className={styles.label} htmlFor="responsible">
                Ответственный пользователь<span className={styles.asterisk}>*</span>
              </label>
              <select
                id="responsible"
                className={styles.select}
                style={{ WebkitAppearance: 'none', MozAppearance: 'none', appearance: 'none' }}
                value={responsibleId ?? currentUser?.id ?? ''}
                onChange={(e) => setResponsibleId(Number(e.target.value) || null)}
              >
                {currentUser ? (
                  <option value={currentUser.id}>{currentUser.fullName}</option>
                ) : (
                  <option value="">Не назначен</option>
                )}
              </select>
            </div>
          </div>

          {/* 3. Наименование объекта * */}
          <div className={styles.field}>
            <label className={styles.label} htmlFor="objectName">
              Наименование объекта<span className={styles.asterisk}>*</span>
            </label>
            <input
              id="objectName"
              type="text"
              className={styles.input}
              placeholder="Многоквартирный жилой дом"
              value={objectName}
              onChange={(e) => setObjectName(e.target.value)}
            />
          </div>

          {/* 4. Адрес (местоположение) * */}
          <div className={styles.field}>
            <label className={styles.label} htmlFor="address">
              Адрес (местоположение)<span className={styles.asterisk}>*</span>
            </label>
            <input
              id="address"
              type="text"
              className={styles.input}
              placeholder="г. Екатеринбург, ул. Красина, 4"
              value={addressText}
              onChange={(e) => setAddressText(e.target.value)}
            />
          </div>

          {/* 5. Ряд из двух колонок: Вид работ* и Дата создания* */}
          <div className={styles.row}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="workType">
                Вид работ<span className={styles.asterisk}>*</span>
              </label>
              <select
                id="workType"
                className={styles.select}
                style={{ WebkitAppearance: 'none', MozAppearance: 'none', appearance: 'none' }}
                value={workType}
                onChange={(e) => setWorkType(e.target.value)}
              >
                <option value="Строительство">Строительство</option>
                <option value="Реконструкция">Реконструкция</option>
                <option value="Капитальный ремонт">Капитальный ремонт</option>
                <option value="Снос">Снос</option>
                <option value="Иные виды работ">Иные виды работ</option>
              </select>
            </div>

            <div className={styles.field}>
              <label className={styles.label} htmlFor="createdAt">
                Дата создания<span className={styles.asterisk}>*</span>
              </label>
              <input
                id="createdAt"
                type="text"
                className={`${styles.input} ${styles.inputDisabled}`}
                value={todayFormatted}
                disabled
                readOnly
              />
            </div>
          </div>

          {/* Кнопки действий: Отмена и Создать проект */}
          <div className={styles.actions}>
            <NavLink to="/projects" className={styles.cancelButton}>
              Отмена
            </NavLink>
            <button
              type="submit"
              className={styles.submitButton}
              disabled={loading}
            >
              {loading ? 'Создание...' : 'Создать проект'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default NewProjectPage;