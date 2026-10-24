# Memorandum
Веб-сервис формирования пояснительной записки проектной документации в формате XML.

## Быстрый старт

**Dev (SQLite, всё локально):**

```bash
docker compose -f infra/docker-compose.dev.yml up     # фронт :5173, API :5000, Scalar :5000/scalar/v1
```

Без Docker: `cd backend && dotnet run --project src/Memorandum.Api`, затем `cd frontend && npm i && npm run dev`.

**Prod (PostgreSQL + nginx):**

```bash
cp infra/.env.example infra/.env && chmod 600 infra/.env    # задать POSTGRES_PASSWORD, APP_HOST
docker compose -f infra/docker-compose.prod.yml up -d --build
```

Инфраструктура, dev/prod-стек и разбор типовых проблем — [`infra/README.md`](infra/README.md).

### Проверка разделения сред

| Проверка | Dev | Prod |
|---|---|---|
| `GET /api/health` → `environment` | `Development`, `database: Sqlite` | `Production`, `database: Postgres` |
| `GET /mock/api/health` | 200 (mock-данные) | 404 |
| `/scalar/v1` | 200 | 404 (включается `OpenApi__Enabled=true`) |
| старт без строки подключения к Postgres | ок | падение с понятной ошибкой |
