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

### CI

Только проверки на PR — ничего не публикуется и не разворачивается (CD будет отдельным этапом).

`.github/workflows/ci.yml`, 4 параллельных джоба:

| Джоб | Что проверяет |
|---|---|
| `backend` | `restore` → `build -c Release -warnaserror` → 12 интеграционных тестов → trx в артефакты |
| `frontend` | `npm ci` → `tsc --noEmit` → `vite build` |
| `actionlint` | линт самих workflow-файлов (официальный образ, включает shellcheck) |
| `compose + dockerfile` | `docker compose config -q` для dev и prod, `bash -n infra/prod-up.sh` |

Ничего не нужно настраивать: ни секретов, ни сервера. Первый прогон — на первом PR.

**Защита ветки `main`** (Settings → Branches → Add branch protection rule):
- Branch name pattern: `main`
- ✅ Require a pull request before merging (1 approval не обязателен для соло-проекта)
- ✅ **Require status checks to pass before merging** → выбрать `backend — build + tests`, `frontend — typecheck + build`, `actionlint`, `compose + dockerfile`
- ✅ Require branches to be up to date before merging
- ✅ Do not allow bypassing the above settings

Без этой настройки workflow только светит красным, но не блокирует merge — включать обязательно.

### Тесты

```bash
dotnet test backend/Memorandum.slnx        # интеграционные: версионирование, ошибки, guard-проверки прод-конфига
```

Тесты поднимают реальное приложение в памяти (`WebApplicationFactory`), без сети и docker.

### Проверка разделения сред

| Проверка | Dev | Prod |
|---|---|---|
| `GET /api/health` → `environment` | `Development`, `database: Sqlite` | `Production`, `database: Postgres` |
| `GET /mock/api/health` | 200 (mock-данные) | 404 |
| `/scalar/v1` | 200 | 404 (включается `OpenApi__Enabled=true`) |
| старт без строки подключения к Postgres | ок | падение с понятной ошибкой |
