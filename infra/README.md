# infra — два окружения

| Файл | Что поднимает | Куда смотреть |
|---|---|---|
| `docker-compose.dev.yml` | api (`dotnet watch`, SQLite) + web (Vite dev-server) | фронт `:5173`, API `:5000`, Scalar `:5000/scalar/v1` |
| `docker-compose.prod.yml` | nginx (SPA + прокси) + api (Postgres) + db + опц. certbot | `:80` (и `:443` с профилем `tls`) |

## DEV

```bash
docker compose -f infra/docker-compose.dev.yml up
```

- бэкенд перезапускается сам при изменении `backend/**` (SQLite-файл лежит в `backend/src/Memorandum.Api/app.db`);
- фронт — горячая перезагрузка, `/api` и `/mock` проксируются на `api:5000`;
- Scalar открыт на `http://localhost:5000/scalar/v1`.

Без Docker:

```bash
# терминал 1
cd backend && dotnet run --project src/Memorandum.Api        # http://localhost:5000
# терминал 2
cd frontend && npm install && npm run dev             # http://localhost:5173
```

## PROD

```bash
cp infra/.env.example infra/.env && $EDITOR infra/.env && chmod 600 infra/.env
docker compose -f infra/docker-compose.prod.yml up -d --build
curl -fsS http://localhost/api/health
```

TLS (нужен домен в `APP_HOST`, A-запись на сервер):

```bash
docker compose -f infra/docker-compose.prod.yml --profile tls up -d
# после выпуска сертификата — раскомментировать блок 443 в infra/nginx/default.conf.template
# и перезапустить web: docker compose -f infra/docker-compose.prod.yml restart web
```

Отличия прода (проверяется автоматически при старте API):
- `Database:Provider` обязан быть `Postgres`, иначе приложение не стартует;
- `ConnectionStrings:Postgres` обязательна;
- `/mock/**` → 404 (и в API, и в nginx);
- Scalar/OpenAPI выключены; включаются флагом `OpenApi__Enabled=true` — удобно на демо/стенде:

```bash
docker compose -f infra/docker-compose.prod.yml exec api printenv OpenApi__Enabled
# включить: добавить в docker-compose.prod.yml в environment: OpenApi__Enabled: "true"
```

## Файлы

```
infra/
├─ docker-compose.dev.yml     dev: api + vite
├─ docker-compose.prod.yml    prod: web + api + db (+ certbot, профиль tls)
├─ api.Dockerfile             publish API, non-root
├─ web.Dockerfile             build Vite → nginx + статика
├─ nginx/default.conf.template  SPA, /api → api:8080, /mock → 404, ACME, healthz
├─ nginx/snippets/proxy.conf  заголовки прокси
└─ .env.example               переменные для prod
```
