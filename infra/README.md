# infra — два окружения

| Файл | Что поднимает | Куда смотреть |
|---|---|---|
| `docker-compose.dev.yml` | api (`dotnet watch`, SQLite) + web (Vite dev-server) | фронт `:5173`, API `:5000`, Scalar `:5000/scalar/v1` |
| `docker-compose.prod.yml` | nginx (SPA + прокси) + api (Postgres) + db + опц. certbot | `:80` (и `:443` с профилем `tls`) |

## DEV

```bash
docker compose -f infra/docker-compose.dev.yml up
```

- бэкенд перезапускается сам при изменении `backend/**`;
- фронт — горячая перезагрузка, `/api` и `/mock` проксируются на `api:5000`;
- Scalar открыт на `http://localhost:5000/scalar/v1`.

Без Docker:

```bash
# терминал 1
cd backend && dotnet run --project src/Memorandum.Api        # http://localhost:5000
# терминал 2
cd frontend && npm install && npm run dev             # http://localhost:5173
```

## PROD — с нуля одной командой

```bash
./infra/prod-up.sh                                   # собрать, поднять, дождаться healthy
APP_HOST=memorandum.example.ru ./infra/prod-up.sh --tls     # + Let's Encrypt (нужен домен)
./infra/prod-up.sh --reset                           # снести данные и поднять заново
./infra/prod-up.sh --rebuild                         # пересобрать образы без кэша
```

Скрипт создаёт `infra/.env` с сгенерированным паролем БД (chmod 600), проверяет docker/compose
(не устанавливает), собирает образы и ждёт ответа `/api/health`.

Вручную:

```bash
cp infra/.env.example infra/.env && $EDITOR infra/.env && chmod 600 infra/.env
docker compose -f infra/docker-compose.prod.yml up -d --build
curl -fsS http://localhost/api/health
```

TLS — отдельный файл `infra/docker-compose.tls.yml`, подключается поверх прод-стека.
Нужен домен в `APP_HOST` и A-запись на сервер.

```bash
# одной командой: certbot выпустит сертификат, затем web перезапустится на 443
APP_HOST=example.ru ./infra/prod-up.sh --tls

# вручную
docker compose -f infra/docker-compose.prod.yml -f infra/docker-compose.tls.yml \
  --env-file infra/.env --profile tls up -d
docker compose -f infra/docker-compose.prod.yml logs certbot    # проверить выпуск
docker compose -f infra/docker-compose.prod.yml \
  -f infra/docker-compose.tls.yml --env-file infra/.env restart web
```

Override добавляет порт 443 и монтирует `infra/nginx/tls/tls.conf.template` в
`/etc/nginx/templates` — server-блок 443 появляется в конфиге только вместе с ним.
Без override стек работает по HTTP и сертификат ему не нужен, поэтому деплой из CI
(без TLS) ничего не ломает. HSTS есть только в HTTPS-блоке.

Отличия прода (проверяется автоматически при старте API):
- `Database:Provider` обязан быть `Postgres`, иначе приложение не стартует;
- `ConnectionStrings:Postgres` обязательна;
- `/mock/**` → 404 (и в API, и в nginx);
- Scalar/OpenAPI выключены; включаются флагом `OpenApi__Enabled=true` — удобно на демо/стенде:

```bash
docker compose -f infra/docker-compose.prod.yml exec api printenv OpenApi__Enabled
# включить: добавить в docker-compose.prod.yml в environment: OpenApi__Enabled: "true"
```

## CD (только VPS)

Локально ничего не нужно: dev — `docker compose -f infra/docker-compose.dev.yml up`,
репетиция прода — `./infra/prod-up.sh` (собирает образы на месте).

На VPS деплой запускается кнопкой **Actions → Deploy** (`workflow_dispatch` в Environment
`production`, требует подтверждения). Workflow делает на сервере:

1. **зависимости** — проверяет `git`, `curl`, `docker`, `docker compose`; ставит только
   недостающее (тогда нужен `sudo` без пароля), включает демон, добавляет в группу `docker`;
2. `docker login ghcr.io` — если заданы `GHCR_USER` и `GHCR_TOKEN`;
3. `git reset --hard` на **тот же** sha, что уходит в тег образа;
4. `pull` → `up -d --wait` → smoke (`/`, health, `Production`, версия = sha, mock → 404);
5. успех — `infra/.deployed_tag` и `infra/.previous_tag`; провал smoke — откат на предыдущий тег
   и повторный smoke.

Порт для smoke берётся из `WEB_PORT` в `infra/.env`. `prod-up.sh` — вспомогательный:
зависимостей не ставит, только проверяет и подсказывает.

Что должно быть на сервере один раз:

```bash
git clone https://github.com/maks-march/Memorandum /opt/memorandum
cd /opt/memorandum && cp infra/.env.example infra/.env && chmod 600 infra/.env
docker login ghcr.io                           # PAT classic, read:packages
```

Секреты workflow: `DEPLOY_HOST`, `DEPLOY_USER`, `SSH_KEY` (ed25519), `GHCR_USER`, `GHCR_TOKEN`.

## Файлы

```
infra/
├─ docker-compose.dev.yml     dev: api + vite
├─ docker-compose.prod.yml    prod: web + api + db (+ certbot, профиль tls)
├─ api.Dockerfile             publish API, non-root
├─ web.Dockerfile             build Vite → nginx + статика
├─ nginx/default.conf.template  SPA, /api → api:8080, /mock → 404, ACME
├─ docker-compose.tls.yml     TLS поверх прода: порт 443 + server-блок 443
├─ nginx/tls/tls.conf.template  server-блок 443 (монтируется только с tls.yml)
├─ nginx/snippets/proxy.conf  заголовки прокси
├─ prod-up.sh                 вспомогательный: локальный запуск прода (проверка зависимостей, .env, сборка)
└─ .env.example               переменные для prod (в т.ч. образы GHCR)
```
