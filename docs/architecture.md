# 01. Архитектура (кратко)

## 0. Текущее состояние (что уже в репозитории)

| Есть | Нет (по плану) |
|---|---|
| API на контроллерах: `/api/health`, `/api/v1/system/info`, `/mock/api/health` | бизнес-эндпоинты: организации, проекты, документы, проверки, импорт |
| Версионирование сегментом URL (`/api/v1/...`), OpenAPI + Scalar по документу на версию | EF Core: модель документов, миграции |
| Единый обработчик ошибок: `ProblemDetails` + `traceId`, `application/problem+json` | XML-ядро, XSD-валидация, предпросмотр, выгрузка |
| Dev/prod-разделение с fail-fast проверками конфигурации на старте; SQLite в dev, Postgres в prod | аутентификация, роли, изоляция организаций |
| 18 интеграционных тестов (`backend/tests/Memorandum.Tests`) | фронтенд-экраны (сейчас один тестовый экран health) |
| CI на PR (5 джобов, включая сборку образов и smoke-прогон прода) + ruleset `main` | HTTPS с доменом, мониторинг, бэкапы по расписанию |
| CD только для прода: образы в GHCR (`release.yml`), деплой на VPS по кнопке (`deploy.yml`: reset → pull → up --wait → smoke); версия сборки = git sha в `/api/health` | dev деплоится через compose |
| Два compose (dev/prod), nginx, `prod-up.sh` | HTTPS с доменом, мониторинг, бэкапы по расписанию |

Разделы ниже описывают **целевую** архитектуру: контейнеры и таблицы с пометкой *(future)*
в текущей фазе не поднимаются.

## 1. Контейнеры

| Контейнер | Образ | Роль | Порты | Состояние |
|---|---|---|---|---|
| `nginx` | `nginx:1.27-alpine` | TLS-терминация, раздача SPA, reverse proxy + балансировка `api`, лимиты тела/рейтов, gzip | 80, 443 → наружу | конфиг в git, сертификаты в volume |
| `api` ×1..N | `mcr.microsoft.com/dotnet/aspnet:10.0` (multi-stage из `sdk:10.0` + `node:22`) | REST API + XML-ядро; SPA-сборка лежит в `wwwroot` (используется только в dev, в prod отдаёт nginx) | 8080 (внутр.) | stateless ⇒ можно `--scale api=2` |
| `db` | `postgres:17-alpine` | все структурированные данные, JSONB-модель документов, XSD-комплекты | 5432 (внутр.) | volume `pgdata` |
| `backup` | `postgres:17-alpine` (cron в контейнере или host cron) | `pg_dump -Fc` + tar `/data` → volume `backups` | — | — |
| *(future)* `redis` | `redis:7-alpine` | кэш `XmlSchemaSet`, сессии/rate-limit, hot-данные | 6379 (внутр.) | AOF |
| *(future)* `kafka` | `bitnami/kafka` (+KRaft) | шина событий `document.verified`, `schema.updated`, аудит/уведомления | 9092 (внутр.) | volume |
| *(future)* `worker` | тот же образ что `api` | consumer kafka: уведомления, перегенерация, отчёты | — | — |
| *(future)* `prometheus` | `prom/prometheus` | сбор метрик (pull, 15s), 15 дней | 9090 (внутр.) | volume `prom_data` |
| *(future)* `grafana` | `grafana/grafana` | дашборды; доступ только через SSH-туннель | 127.0.0.1:3000 | volume `grafana_data` |
| *(future)* `alertmanager` | `prom/alertmanager` | алерты в Telegram | 9093 (внутр.) | — |
| *(future)* `node_/postgres_/nginx_exporter` | prom-* | метрики хоста, БД, nginx | 91xx (внутр.) | — |

Наружу открыты **только 80/443 nginx**. Всё остальное — внутренняя docker-сеть.

## 2. Языки, фреймворки, библиотеки

| Слой | Технологии |
|---|---|
| Frontend | React 19, TypeScript 5.x, Vite, React Router, Tailwind CSS, shadcn/ui (Radix), TanStack Query, TanStack Table, react-hook-form + zod, dnd-kit, lucide-react |
| Backend | .NET 10 LTS, ASP.NET Core **контроллеры** (жирные модели, без слоёв сервисов/репозиториев), EF Core 10 + Npgsql, FluentValidation, встроенный OpenAPI + Scalar |
| XML | только BCL: `System.Xml.Linq` (`XDocument`), `System.Xml.Schema` (`XmlSchemaSet`, `XmlSchemaValidator`), `System.Xml.Xsl`, собственный `PathTrackingXmlReader` |
| БД | PostgreSQL 17 (`jsonb`, `pg_trgm`, `citext`, `pgcrypto`) |
| Тесты | xUnit + `WebApplicationFactory` (интеграционные, уже есть), позже Testcontainers (Postgres), Verify (снапшоты XML), Playwright, `xmllint` (libxml2) |
| Инфра | Docker, Docker Compose, nginx, GitHub Actions (CI + ruleset), Ubuntu 24.04 VPS |

## 3. Статика и хранилища

| Что | Где |
|---|---|
| SPA (`/`, `/assets/*`, `index.html`) | сборка Vite → `frontend/dist` → **nginx** (prod), Vite dev-server (dev). Кэш: `immutable` на `/assets`, `no-cache` на `index.html` |
| API | `location /api/` → `proxy_pass http://api_upstream` (балансировка, `least_conn`) |
| JSONB-модель документов, версии, проверки, пользователи | PostgreSQL |
| Комплекты XSD (все файлы схемы + xsl) | `schema_package_files.content bytea` в БД + копия в `schemas/` в git с `sha256` |
| Исходные импортированные XML | `document_imports.original_xml bytea` (гарантия «без потери данных») |
| Вложения разделов ПД, выгрузки | volume `/data` (локальный диск); позже S3-совместимое (MinIO) |
| Логи | stdout → docker json-file (rotate 20 МБ × 5) + `app_errors` в БД (для админки); позже Loki |
| Метрики | Prometheus (`/metrics` на порту 8081, не проксируется nginx) → Grafana |
| Бэкапы | `/backups` (внешний volume или другой хост), 14 дней, `pg_dump -Fc` + tar |

## 4. Путь запроса (middleware pipeline)

```
Клиент (SPA)
  │ HTTPS 443
  ▼
nginx
  ├─ TLS-терминация, HTTP→HTTPS, HSTS, gzip/brotli, лимит тела 20 МБ
  ├─ security-заголовки (CSP, nosniff, X-Frame-Options) — snippet в каждом location
  ├─ /assets/*, /favicon.ico  → отдаёт файл с диска (не доходит до .NET)
  ├─ /api/*, /health          → upstream api (least_conn, keepalive 32, retry 1)
  └─ /*                       → index.html (SPA fallback)
  ▼
Kestrel (ASP.NET Core, :8080, UseForwardedHeaders → реальный IP и https-схема)
  ▼
1.  UseSerilogRequestLogging        — request id, длительность, статус
2.  UseExceptionHandler             — 500 → ProblemDetails, запись в app_errors
3.  UseHsts / UseHttpsRedirection
4.  UseRateLimiter                  — /auth/login, /auth/register: 10/мин на IP
5.  UseStaticFiles                  — dev-режим: SPA из wwwroot
6.  UseRouting
7.  UseAuthentication               — cookie-ticket (HttpOnly, Secure, SameSite=Lax)
8.  UseAuthorization                — политики Admin | OrgAdmin | Editor | Viewer
9.  Antiforgery                     — для POST/PATCH/DELETE
10. CurrentUserMiddleware           — userId, orgId, role → scope на запрос
11. OrgScopeFilter (EF global query filter) — изоляция по org_id
12. Endpoint (контроллер + жирная модель)
      ├─ FluentValidation DTO
      ├─ EF Core → PostgreSQL
      └─ XML-ядро → FieldCatalog → IXmlAdapter(Build/Parse) → XmlSchemaSet(validate) → Preview
  ▼
ProblemDetails | JSON  → клиент
```

XML-ядро (отдельная сборка `Memorandum.Xml`) не знает ни про ASP.NET, ни про EF: вход — модель, выход — `XDocument` / `ValidationResult`. Тестируется без веба.

## 5. Будущее (заложено в дизайн, не реализуется в v1)

| Компонент | Зачем | Точка внедрения |
|---|---|---|
| **Redis** | кэш скомпилированных `XmlSchemaSet` (сборка дорогая), кэш `form-manifest`, rate-limit счётчики, распределённые сессии при `api` > 1 | `ISchemaSetCache`, `manifest`-эндпоинт, `IDistributedRateLimiter` |
| **Kafka** | события домена: `document.verified`, `document.exported`, `schema.published`, `user.invited` → уведомления, аудит, регенерация, синхронизация с внешними системами | публикация из хендлеров через `IEventBus`; consumer — отдельный `worker` |
| **Бэкапы БД** | ежедневный `pg_dump -Fc` + PITR при росте | сервис `backup` в `docker-compose.prod.yml`; при росте — `pgBackRest`/WAL на S3 |

## 6. Мощности

### Конфигурации

| Среда | vCPU | RAM | Диск | Где | Комментарий |
|---|---|---|---|---|---|
| Dev (ноутбук) | 2 | 8 | 20 | docker compose (db + api) | Vite dev-server, Postgres в контейнере |
| **Prod min (v1, демо/защита)** | **2** | **4** | **40 SSD** | 1 VPS ~500–800 ₽/мес | nginx 0.1 + api 0.5 ГБ + postgres 1 ГБ + запас; до ~50 организаций |
| Prod + Docker build on host | 2 | 4 | 40 | тот же | сборка образа ~1.5–2 ГБ RAM, лучше билдить в CI |
| Prod + Redis | 2 | 4–6 | 40 | тот же | Redis 256 МБ RAM |
| Prod + Redis + Kafka | 4 | 8 | 80 | 1–2 VPS | Kafka в KRaft жрёт ~1 ГБ RAM |
| Рост (>500 польз.) | 4–8 | 16 | 160 | 2 VPS: app + db | `api` ×2 за nginx, БД отдельно, бэкапы на S3 |
| K8s | — | — | — | не нужен для учебного проекта | избыточно |

### Лимиты, которые надо выставить
- API: max 50 concurrent requests, `MaxRequestBodySize` 20 МБ, таймаут валидации 10 с;
- Postgres: `shared_buffers=512MB`, `work_mem=16MB`, `max_connections=50`;
- nginx: `client_max_body_size 20m`, `proxy_read_timeout 60s`, gzip ≥ 1 КБ;
- бэкап: 1×/сутки, хранение 14 дней, ~10–30 МБ/дамп на v1.

### Периметр (DNS/сеть)
- 1 домен, A-запись → VPS, `ufw`: 22 (по ключу), 80, 443;
- TLS: Let's Encrypt через certbot на nginx (в compose — отдельный контейнер `certbot` или certbot на хосте);
- продовые секреты — `.env` на сервере (chmod 600), не в git.
