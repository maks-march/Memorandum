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

Проверки на PR. Ничего не публикуется и не разворачивается — этим занимается Release и Deploy (см. ниже).

`.github/workflows/ci.yml`, 5 параллельных джобов:

| Джоб | Что проверяет |
|---|---|
| `backend` | `restore` → `build -c Release` → 18 интеграционных тестов → `dotnet list package --vulnerable` → trx в артефакты |
| `frontend` | `npm ci` → `npm run build` (`tsc -b` + `vite build`, одна проверка типов) |
| `actionlint` | линт самих workflow-файлов |
| `infra` | `shellcheck` (все скрипты), `docker compose config` со всеми профилями (`tls`, `tools`), `nginx -t` на шаблоне после `envsubst`, **сборка обоих образов без push** и запуск api-образа с прод-конфигом (health 200, без `AllowedHosts` — падение) |
| `smoke` | собирает **реальные образы** и поднимает прод-стек: health, версия в URL, 404 на mock, security-заголовки на HTML и ассетах |

Зависимости обновляет Dependabot (`.github/dependabot.yml`): nuget, npm — еженедельно, actions и
docker — ежемесячно.

### Защита ветки main

Правила защиты лежат в `.github/rulesets/main.json` — это версионируемое описание политики.
Применяются одной командой (создать или обновить):

```bash
gh api --method POST repos/{owner}/{repo}/rulesets --input .github/rulesets/main.json
```

Проверить, что применено:

```bash
gh ruleset list
gh ruleset check main
```

Второй раз тот же `POST` вернёт ошибку «already exists» — тогда либо правьте ruleset в
Settings → Rules, либо обновляйте через `PUT` по id.

Ruleset `main` запрещает удаление и force push, обязывает изменения идти через PR и требует
зелёные чеки `backend`, `frontend`, `actionlint`, `infra`, `smoke` на актуальном состоянии `main`.
Bypass-list пуст — обойти не может никто. Разрешены все три метода merge: `Merge commit`,
`Squash`, `Rebase`.

Порядок важен: сначала пушить и дождаться первого зелёного прогона CI, **потом** применять
ruleset — иначе required-чеки ещё не известны GitHub и merge будет заблокирован навсегда.

### CD — только прод

Разработка деплоя не требует: `docker compose -f infra/docker-compose.dev.yml up` или
`./infra/prod-up.sh` для локальной репетиции прода. `prod-up.sh` — вспомогательный: он проверяет,
что Docker на месте, и падает с подсказкой, но **ничего не устанавливает**. Установка зависимостей
на сервере — задача CD.

Для VPS два workflow:

| Что | Файл | Триггер |
|---|---|---|
| Образы в GHCR (`memorandum-api`, `memorandum-web`, теги `sha-<7>` + `stable`) | `release.yml` | push в `main` |
| Деплой на VPS: проверка и установка зависимостей (`git`, `curl`, `docker`, compose v2) → `git reset` → `compose pull` → `up --wait` → smoke | `deploy.yml` | вручную |

Версия сборки — короткий git sha: `/api/health` отдаёт `"version":"1.0.0+a3f9c21"`, и деплой
проверяет, что на сервере именно эта сборка. Секреты сервера — `infra/.env` (chmod 600).

### Тесты

```bash
dotnet test backend/Memorandum.slnx        # интеграционные: версионирование, ошибки, guard-проверки прод-конфига
```

Тесты поднимают реальное приложение в памяти (`WebApplicationFactory`), без сети и docker.
Проверяют версионирование по URL, обработку ошибок (500 + `traceId`), fail-fast прод-конфигурации.

### Проверка разделения сред

| Проверка | Dev | Prod |
|---|---|---|
| `GET /api/health` → `environment` | `Development`, `database: Sqlite` | `Production`, `database: Postgres` |
| `GET /mock/api/health` | 200 (mock-данные) | 404 |
| `/scalar/v1` | 200 | 404 (включается `OpenApi__Enabled=true`) |
| старт без строки подключения к Postgres | ок | падение с понятной ошибкой |
