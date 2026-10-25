#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
#  Memorandum — запуск прода с нуля.
#
#  Что делает:
#    1. проверяет docker + compose v2;
#    2. создаёт infra/.env из .env.example (если нет) и генерирует пароль БД;
#    3. собирает образы и поднимает стек (nginx + api + postgres);
#    4. ждёт, пока /api/health ответит 200;
#    5. печатает адреса и следующие шаги.
#
#  Использование:
#    ./infra/prod-up.sh                  # обычный запуск
#    ./infra/prod-up.sh --tls            # + certbot выпускает сертификат (HTTPS включается вручную)
#    ./infra/prod-up.sh --reset          # снести данные (БД, сертификаты) и поднять заново
#    ./infra/prod-up.sh --rebuild        # принудительно пересобрать образы
#    APP_HOST=memorandum.example.ru ./infra/prod-up.sh
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

# ── пути ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.prod.yml"
ENV_FILE="$SCRIPT_DIR/.env"
ENV_EXAMPLE="$SCRIPT_DIR/.env.example"

# ── флаги ────────────────────────────────────────────────────────────────────
TLS=0
RESET=0
REBUILD=0
for arg in "$@"; do
  case "$arg" in
    --tls)     TLS=1 ;;
    --reset)   RESET=1 ;;
    --rebuild) REBUILD=1 ;;
    -h|--help)
      awk 'NR>1 && /^#/ { sub(/^# ?/, ""); print; next } NR>1 { exit }' "${BASH_SOURCE[0]}"
      exit 0
      ;;
    *) echo "Неизвестный флаг: $arg (см. --help)" >&2; exit 2 ;;
  esac
done

# ── вывод ────────────────────────────────────────────────────────────────────
c_ok()   { printf '\033[32m✓\033[0m %s\n' "$*"; }
c_info() { printf '\033[36m•\033[0m %s\n' "$*"; }
c_warn() { printf '\033[33m!\033[0m %s\n' "$*"; }
c_err()  { printf '\033[31m✗\033[0m %s\n' "$*" >&2; }
step()   { printf '\n\033[1m== %s\033[0m\n' "$*"; }

# ── 1. проверки окружения ────────────────────────────────────────────────────
step "1/5 Проверка окружения"

if ! command -v docker >/dev/null 2>&1; then
  c_err "docker не найден. Установите Docker Engine: https://docs.docker.com/engine/install/"
  exit 1
fi
c_ok "docker: $(docker --version | cut -d, -f1)"

if ! docker compose version >/dev/null 2>&1; then
  c_err "Нужен Docker Compose v2 (команда 'docker compose', не 'docker-compose')."
  exit 1
fi
c_ok "compose: $(docker compose version --short 2>/dev/null || echo v2)"

if ! docker info >/dev/null 2>&1; then
  c_err "Docker-демон не запущен или нет прав. Попробуйте: sudo systemctl start docker"
  c_err "Если пользователь не в группе docker: sudo usermod -aG docker \$USER && newgrp docker"
  exit 1
fi
c_ok "docker-демон доступен"

[ -f "$COMPOSE_FILE" ] || { c_err "не найден $COMPOSE_FILE"; exit 1; }

# ── 2. .env ──────────────────────────────────────────────────────────────────
step "2/5 Файл окружения"

# APP_HOST, переданный в командной строке, имеет приоритет над значением в .env
APP_HOST_OVERRIDE="${APP_HOST:-}"

gen_password() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 24 | tr -d '/+=' | cut -c1-24
  else
    LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24
  fi
}

if [ "$RESET" -eq 1 ]; then
  c_warn "--reset: удаляю контейнеры и тома (БД и сертификаты будут потеряны)"
  # Если .env уже удалён, compose всё равно нужно чем-то подставить обязательные
  # переменные — для down достаточно примера, секреты там не используются.
  down_env="$ENV_FILE"
  if [ ! -f "$down_env" ]; then
    down_env="$ENV_EXAMPLE"
    c_info ".env отсутствует — для остановки использую .env.example"
  fi

  if ! docker compose -f "$COMPOSE_FILE" --env-file "$down_env" down -v --remove-orphans; then
    c_err "не удалось удалить контейнеры и тома (см. вывод выше)."
    c_err "Уберите вручную: docker compose -f $COMPOSE_FILE down -v --remove-orphans"
    exit 1
  fi
  rm -f "$ENV_FILE"
fi

if [ -f "$ENV_FILE" ]; then
  c_ok "использую существующий $ENV_FILE"
  set -a
  # shellcheck source=/dev/null
  . "$ENV_FILE"
  set +a

  if [ -n "$APP_HOST_OVERRIDE" ] && [ "$APP_HOST_OVERRIDE" != "${APP_HOST:-}" ]; then
    grep -q '^APP_HOST=' "$ENV_FILE" \
      && sed -i.bak "s|^APP_HOST=.*|APP_HOST=${APP_HOST_OVERRIDE}|" "$ENV_FILE" \
      || echo "APP_HOST=${APP_HOST_OVERRIDE}" >> "$ENV_FILE"
    rm -f "${ENV_FILE}.bak"
    APP_HOST="$APP_HOST_OVERRIDE"
    export APP_HOST
    c_warn "APP_HOST переопределён из командной строки: ${APP_HOST}"
  fi
else
  [ -f "$ENV_EXAMPLE" ] || { c_err "не найден $ENV_EXAMPLE"; exit 1; }
  DB_PASS="$(gen_password)"
  APP_HOST_VALUE="${APP_HOST:-localhost}"

  sed \
    -e "s|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=${DB_PASS}|" \
    -e "s|^APP_HOST=.*|APP_HOST=${APP_HOST_VALUE}|" \
    "$ENV_EXAMPLE" > "$ENV_FILE"
  chmod 600 "$ENV_FILE"
  c_ok "создан $ENV_FILE (пароль БД сгенерирован)"
  c_warn "securely сохраните секрет: grep POSTGRES_PASSWORD $ENV_FILE"

  set -a
  # shellcheck source=/dev/null
  . "$ENV_FILE"
  set +a
fi

if [ -z "${POSTGRES_PASSWORD:-}" ]; then
  c_err "в $ENV_FILE не задан POSTGRES_PASSWORD"
  exit 1
fi

if [ "${APP_HOST:-localhost}" = "localhost" ]; then
  c_warn "APP_HOST=localhost: подойдёт для проверки на сервере, для домена задайте APP_HOST"
fi
c_ok "APP_HOST=${APP_HOST:-localhost}"

# ── 3. сборка и запуск ───────────────────────────────────────────────────────
step "3/5 Сборка и запуск"

# локальная сборка не должна занимать тег из реестра: образ получит имя :local
export IMAGE_TAG="${IMAGE_TAG:-local}"
export SOURCE_REVISION="${SOURCE_REVISION:-local}"

COMPOSE=(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE")

if [ "$TLS" -eq 1 ]; then
  if [ "${APP_HOST:-localhost}" = "localhost" ]; then
    c_warn "--tls с APP_HOST=localhost: Let's Encrypt не выдаст сертификат на localhost"
  fi
  c_info "включаю профиль tls (certbot)"
  COMPOSE+=(--profile tls)
fi

if [ "$REBUILD" -eq 1 ]; then
  c_info "пересобираю образы без кэша"
  "${COMPOSE[@]}" build --no-cache
else
  c_info "собираю образы (первый запуск может занять 3–10 минут)"
  "${COMPOSE[@]}" build
fi

c_info "поднимаю контейнеры"
"${COMPOSE[@]}" up -d --remove-orphans

# ── 4. ожидание готовности ───────────────────────────────────────────────────
step "4/5 Ожидание готовности API"

wait_http() {
  local url="$1" name="$2" tries="${3:-60}" i
  for ((i = 1; i <= tries; i++)); do
    if curl -fsS -m 3 -o /dev/null "$url" 2>/dev/null; then
      c_ok "$name отвечает (${i}я попытка)"
      return 0
    fi
    sleep 2
  done
  c_err "$name не ответил за $((tries * 2)) с"
  return 1
}

# healthcheck контейнера api
for ((i = 1; i <= 60; i++)); do
  status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' \
    "$("${COMPOSE[@]}" ps -q api 2>/dev/null)" 2>/dev/null || echo unknown)"
  if [ "$status" = "healthy" ]; then
    c_ok "контейнер api: healthy"
    break
  fi
  if [ "$i" -eq 60 ]; then
    c_err "api не стал healthy за 120 с. Логи:"
    "${COMPOSE[@]}" logs --tail 40 api
    exit 1
  fi
  [ "$i" -eq 1 ] && c_info "ждём healthcheck контейнера api…"
  sleep 2
done

# HTTPS пока не включён: certbot только выпускает сертификат.
# Чтобы поднять HTTPS — включите server-блок 443 (infra/nginx/default.conf.template)
# и порт "443:443" в docker-compose.prod.yml.
BASE_URL="http://localhost"
if [ "${APP_HOST:-localhost}" != "localhost" ]; then
  BASE_URL="http://${APP_HOST}"
fi

if ! wait_http "${BASE_URL}/api/health" "GET /api/health" 30; then
  c_err "логи nginx:"; "${COMPOSE[@]}" logs --tail 30 web || true
  exit 1
fi

# ── 5. итог ──────────────────────────────────────────────────────────────────
step "5/5 Готово"

printf '%s\n' "$(curl -fsS "${BASE_URL}/api/health" || true)" | sed 's/^/    /'

cat <<EOF

  Приложение:   ${BASE_URL}
  Health:       ${BASE_URL}/api/health
  Mock (404):   ${BASE_URL}/mock/api/health
  Контейнеры:   ${COMPOSE[*]} ps
  Логи API:     ${COMPOSE[*]} logs -f api

  Полезное:
    docker compose -f infra/docker-compose.prod.yml ps
    docker compose -f infra/docker-compose.prod.yml logs -f
    ./infra/prod-up.sh --reset          # снести данные и поднять заново
    docker compose -f infra/docker-compose.prod.yml down        # остановить (данные остаются)
    docker compose -f infra/docker-compose.prod.yml down -v     # остановить и удалить данные

  Scalar (документация API) в проде выключен. Включить на время демо:
    1) в infra/docker-compose.prod.yml в services.api.environment добавить  OpenApi__Enabled: "true"
    2) docker compose -f infra/docker-compose.prod.yml up -d api
    3) открыть ${BASE_URL}/scalar/v1

EOF

if [ "$TLS" -eq 1 ]; then
  c_warn "сертификат выпускается, но HTTPS ещё НЕ включён (нужен server-блок 443 + порт 443)."
  c_info "проверить выпуск: ${COMPOSE[*]} logs certbot"
fi

c_ok "прод запущен"
