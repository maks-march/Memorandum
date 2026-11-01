# infra — два окружения

| Файл | Что поднимает | Куда смотреть |
|---|---|---|
| `docker-compose.dev.yml` | api (`dotnet watch`, SQLite) + web (Vite dev-server) | фронт `:5173`, API `:5000`, Scalar `:5000/scalar/v1` |
| `docker-compose.prod.yml` | nginx (SPA + прокси) + api (Postgres) + db + опц. certbot | `:80` (и `:443` вместе с `docker-compose.tls.yml`) |

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
sudo ./infra/prod-up.sh --app-host 203.0.113.10              # образы :stable из GHCR + up + smoke
sudo ./infra/prod-up.sh --app-host example.ru --tls          # + Let's Encrypt (нужен домен и A-запись)
sudo ./infra/prod-up.sh --app-host example.ru --tls --cron --ufw
sudo ./infra/prod-up.sh --app-host 203.0.113.10 --local      # собрать образы из этого checkout
sudo ./infra/prod-up.sh --app-host 203.0.113.10 --dry-run    # показать команды
```

Скрипт создаёт `infra/.env` с сгенерированным паролем БД (chmod 600), проверяет docker/compose
(не устанавливает), поднимает стек и ждёт ответа `/api/health`. Без `--local` образы тянутся
из GHCR по `IMAGE_TAG` из `infra/.env`, с `--local` — собираются на месте из git sha:
`SOURCE_REVISION` у api и `APP_VERSION` у web (он же попадает в бейдж версии).

Вручную:

```bash
cp infra/.env.example infra/.env && $EDITOR infra/.env && chmod 600 infra/.env
docker compose -f infra/docker-compose.prod.yml up -d --build
curl -fsS http://localhost/api/health
```

TLS — `infra/docker-compose.tls.yml` поверх прод-стека. Нужен домен в `APP_HOST`
(или публичный IP) и A-запись на сервер.

Порядок бутстрапа важен: `web` сначала поднимается с HTTP-конфигом и отдаёт
ACME-челлендж, `certbot` получает сертификат, и только после этого `web`
пересоздаётся с 443. TLS-конфиг ссылается на `fullchain.pem`, и без сертификата
nginx не стартует вообще — поэтому включить 443 «сразу» нельзя.

```bash
# одной командой: web(HTTP) → certbot → ожидание сертификата → web(443) → smoke по https
./infra/prod-up.sh --app-host example.ru --tls

# вручную
docker compose -f infra/docker-compose.prod.yml --env-file infra/.env up -d db api web
docker compose -f infra/docker-compose.prod.yml -f infra/docker-compose.tls.yml \
  --env-file infra/.env up -d --no-deps certbot
docker compose -f infra/docker-compose.prod.yml -f infra/docker-compose.tls.yml \
  --env-file infra/.env logs -f certbot          # дождаться сертификата
docker compose -f infra/docker-compose.prod.yml -f infra/docker-compose.tls.yml \
  --env-file infra/.env up -d --force-recreate web
```

Состояние режима — `TLS_ENABLED` в `infra/.env`: его ставит скрипт после успешного
smoke по https, читают и скрипт, и `deploy.yml` (иначе деплой по кнопке пересоздал бы
web без оверрайда и 443 бы пропал). Откат на HTTP: поставьте `TLS_ENABLED=0` в
`infra/.env` и запустите `./infra/prod-up.sh --app-host <host>` без `--tls` — веб
пересоздастся с HTTP-конфигом, 443 закроется.

Override добавляет 443, тома `certs`/`certbot_www`, сервис `certbot` и подменяет
шаблон nginx на `infra/nginx/tls/default.conf.template` — в нём сразу и ACME на 80,
и `301` на 443, и HTTPS-блок с HSTS (`snippets/hsts.conf` в каждом location).
`/healthz` — техническая ручка для healthcheck контейнера, не публичный монитор.

Certbot: домен — `-d $APP_HOST`, renew раз в 12 ч; IP — `--ip-address $APP_HOST
--preferred-profile shortlived` (сертификаты на IP живут ~6 дней), renew раз в сутки.
Обновление само nginx не перезагружает: `--deploy-hook` трогает
`/var/www/certbot/.reload`, а хост-cron с `infra/reload-certs.sh` делает
`nginx -s reload` только при наличии флага. При `--tls` этот cron ставится
автоматически (04:00) — без него nginx держит старый сертификат в памяти, пока
тот не истечёт. Скрипт также ругается в лог, если сертификату осталось меньше
3 дней.

Отличия прода (проверяется автоматически при старте API):
- `Database:Provider` обязан быть `Postgres`, иначе приложение не стартует;
- `ConnectionStrings:Postgres` обязательна;
- `/mock/**` → 404 (и в API, и в nginx);
- Scalar/OpenAPI выключены; включаются флагом `OpenApi__Enabled=true` — удобно на демо/стенде.
  nginx проксирует `/scalar` и `/openapi` в API, поэтому демо открывается по адресу
  `https://APP_HOST/scalar/v1`. Не оставляйте включённым постоянно: спека становится публичной.

```bash
docker compose -f infra/docker-compose.prod.yml exec api printenv OpenApi__Enabled
# включить: добавить в docker-compose.prod.yml в environment: OpenApi__Enabled: "true"
```

## CD (только VPS)

Dev деплоя не требует: `docker compose -f infra/docker-compose.dev.yml up`.

Первый запуск на сервере — `prod-up.sh` (он же готовит машину):

```bash
sudo ./infra/prod-up.sh --app-host 203.0.113.10
sudo ./infra/prod-up.sh --app-host example.ru --ssh-key ~/.ssh/deploy.pub --ufw --cron
sudo ./infra/prod-up.sh --app-host 203.0.113.10 --dry-run     # только показать команды
```

Флаги: `--app-host` (обязателен), `--web-port`, `--path` (по умолчанию `/opt/memorandum`),
`--branch`, `--user`, `--ssh-key`, `--tls`, `--local`, `--ufw`, `--cron`, `--skip-up`,
`--force`, `--dry-run`.
Скрипт не пушит в git и правит только `infra/.env` (он в `.gitignore`). Учтите, что
обновление кода идёт через `git reset --hard origin/<branch>` — незакоммиченные
правки на сервере будут потеряны.

На VPS деплой запускается кнопкой **Actions → Deploy** (`workflow_dispatch` в Environment
`production`, требует подтверждения). Workflow делает на сервере:

1. **зависимости** — проверяет `git`, `curl`, `docker`, `docker compose`; ставит только
   недостающее (тогда нужен `sudo` без пароля), включает демон, добавляет в группу `docker`;
2. `docker login ghcr.io` — если заданы `GHCR_USER` и `GHCR_TOKEN`;
3. `git reset --hard` на **тот же** sha, что уходит в тег образа;
4. `pull` → `up -d --wait` → smoke (`/`, health, `Production`, версия = sha, mock → 404);
5. успех — `infra/.deployed_tag` и `infra/.previous_tag`; провал smoke — `git reset`
   на коммит предыдущего тега, откат образов и повторный smoke.

Порт для smoke берётся из `WEB_PORT`, для HTTPS — из `HTTPS_PORT` в `infra/.env`.

Порядок первого запуска: `prod-up.sh` (готовит сервер, клон, `infra/.env`, стек) →
`docker login ghcr.io`, если пакеты приватные → Actions → Deploy.

Секреты workflow: `DEPLOY_HOST`, `DEPLOY_USER`, `SSH_KEY` (ed25519), `GHCR_USER`, `GHCR_TOKEN`.

## Файлы

```
infra/
├─ docker-compose.dev.yml     dev: api + vite
├─ docker-compose.prod.yml    prod: web + api + db + backup (профиль tools)
├─ api.Dockerfile             publish API, non-root
├─ web.Dockerfile             build Vite → nginx + статика
├─ nginx/default.conf.template  SPA, /api и /scalar|/openapi → api:8080, /mock → 404, ACME
├─ docker-compose.tls.yml     TLS поверх прода: 443, сертификаты, certbot
├─ nginx/tls/default.conf.template  подменяет HTTP-шаблон в TLS-режиме (80 → 443, HSTS, /healthz)
├─ reload-certs.sh            nginx -s reload после продления сертификата + проверка срока
├─ nginx/snippets/proxy.conf  заголовки прокси
├─ nginx/snippets/hsts.conf   HSTS для HTTPS-блоков
├─ nginx/snippets/security-headers-docs.conf  CSP для /scalar (шрифты Scalar)
├─ prod-up.sh                 подготовка сервера и первый запуск прода (пакеты, клон, .env, smoke)
└─ .env.example               переменные для prod (в т.ч. образы GHCR)
```
