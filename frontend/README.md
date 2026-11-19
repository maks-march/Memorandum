## 1. Стек технологий
- **Фреймворк**: React 19 + TypeScript 5.9
- **Сборщик**: Vite 8
- **Стилизация**: **CSS Modules** (`*.module.css`) для каждого компонента + глобальные CSS-токены из Figma в `src/styles.css`
- **Маршрутизация**: React Router v7 (`react-router-dom`)

---

## 2. Старт
```bash
# Зависимости 
npm install

# Запуск локалки
npm run dev

# продакщен-сборка
npm run build
```
---

## 3. Текущая структура проекта

```text
frontend/
├── src/
│   ├── components/
│   │   └── layout/
│   │       ├── Header/             # Верхний хедер (Вариант Б)
│   │       │   ├── Header.tsx
│   │       │   └── Header.module.css
│   │       └── AppLayout/          # Общий макет со сквозным хедером
│   │           ├── AppLayout.tsx
│   │           └── AppLayout.module.css
│   ├── pages/
│   │   └── HomePage/               # Главная страница
│   │       ├── HomePage.tsx
│   │       └── HomePage.module.css
│   ├── types/
│   │   └── css.d.ts                # Декларация типов для *.module.css
│   ├── api.ts                      # Запросы health-проверки и REST API бекенда
│   ├── App.tsx                     # Корневой маршрутизатор (роутинг)
│   ├── main.tsx                    # Точка входа в React DOM
│   └── styles.css                  # Глобальные токены палитры и сброс отступов
├── public/
│   └── favicon.svg                 # Иконка вкладки <>
├── index.html                      # Каркас HTML с подключенным шрифтом Inter
├── package.json                    # Зависимости и скрипты
├── tsconfig.app.json               # Настройки TypeScript
└── vite.config.ts                  # Конфигурация Vite и прокси к API бекенда (:5000)
```
---
