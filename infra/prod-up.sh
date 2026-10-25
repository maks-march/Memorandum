#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
#  Memorandum — подготовка сервера и первый запуск прода.
#
#  Делает всё, что нужно один раз: пакеты, docker, клон, infra/.env, pull,
#  запуск, smoke. Ничего не пушит в git и не меняет отслеживаемые файлы
#  (кроме infra/.env — он в .gitignore), поэтому следующий CD-деплой
#  с git reset --hard ничего не сломает.
#
#  Использование:
#    sudo ./server-prepare.sh --app-host 203.0.113.10
#    sudo ./server-prepare.sh --app-host pp7.example.ru --web-port 80 --ufw --cron
#    sudo ./server-prepare.sh --app-host 203.0.113.10 --dry-run    # только показать
#
#  Флаги:
#    --app-host HOST     домен или IP сервера (обязательно; попадает в AllowedHosts)
#    --web-port N        порт наружу, по умолчанию 80
#    --path DIR          каталог клона, по умолчанию /opt/memorandum
#    --branch BR         ветка, по умолчанию main
#    --user USER         владелец клона и деплой-пользователь (по умолчанию — текущий)
#    --ssh-key FILE      добавить публичный ключ в authorized_keys этого пользователя
#    --ufw               настроить ufw: 22, WEB_PORT и 443
#    --cron              поставить бэкап БД в cron (03:00)
#    --skip-up           только подготовка, без pull/up/smoke
#    --force             перезаписать infra/.env (пароль БД будет новым!)
#    --dry-run           показать команды, ничего не выполнять
#    -h | --help
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

REPO_URL="https://github.com/maks-march/Memorandum.git"
BRANCH="main"
TARGET="/opt/memorandum"
APP_HOST=""
WEB_PORT="80"
DEPLOY_USER=""
SSH_KEY_FILE=""
SETUP_UFW=0
SETUP_CRON=0
DO_UP=1
FORCE=0
DRY=0

# ── вывод ────────────────────────────────────────────────────────────────────
c_ok()   { printf '\033[32m✓\033[0m %s\n' "$*"; }
c_info() { printf '\033[36m•\033[0m %s\n' "$*"; }
c_warn() { printf '\033[33m!\033[0m %s\n' "$*"; }
c_err()  { printf '\033[31m✗\033[0m %s\n' "$*" >&2; }
step()   { printf '\n\033[1m== %s\033[0m\n' "$*"; }

usage() { awk 'NR>1 && /^#/ { sub(/^# ?/, ""); print; next } NR>1 { exit }' "${BASH_SOURCE[0]}"; }

# ── разбор флагов ────────────────────────────────────────────────────────────
while [ $# -gt 0 ]; do
  case "$1" in
    --app-host) APP_HOST="${2:-}"; shift 2 ;;
    --web-port) WEB_PORT="${2:-}"; shift 2 ;;
    --path)     TARGET="${2:-}";   shift 2 ;;
    --branch)   BRANCH="${2:-}";   shift 2 ;;
    --user)     DEPLOY_USER="${2:-}"; shift 2 ;;
    --ssh-key)  SSH_KEY_FILE="${2:-}"; shift 2 ;;
    --ufw)      SETUP_UFW=1; shift ;;
    --cron)     SETUP_CRON=1; shift ;;
    --skip-up)  DO_UP=0; shift ;;
    --force)    FORCE=1; shift ;;
    --dry-run)  DRY=1; shift ;;
    -h|--help)  usage; exit 0 ;;
    *) c_err "неизвестный флаг: $1 (см. --help)"; exit 2 ;;
  esac
done

# ── обёртка запуска (уважает --dry-run) ──────────────────────────────────────
run() {
  if [ "$DRY" -eq 1 ]; then
    printf '   \033[90m$ %s\033[0m\n' "$*"
  else
    "$@"
  fi
}
run_sh() {  # для пайпов и составных команд
  if [ "$DRY" -eq 1 ]; then
    printf '   \033[90m$ %s\033[0m\n' "$*"
  else
    bash -c "$*"
  fi
}

# ── 0. права ─────────────────────────────────────────────────────────────────
if [ "$(id -u)" -eq 0 ]; then
  SUDO=""
else
  if ! command -v sudo >/dev/null 2>&1; then
    c_err "нужен root или sudo"; exit 1
  fi
  SUDO="sudo"
fi

[ -n "$DEPLOY_USER" ] || DEPLOY_USER="${SUDO_USER:-$(id -un)}"

if [ -z "$APP_HOST" ]; then
  c_err "не задан --app-host (IP или домен сервера)"
  c_err "пример: $0 --app-host 203.0.113.10"
  exit 2
fi
case "$APP_HOST" in
  *'*'*) c_err "APP_HOST не может содержать '*': fail-fast в API это запрещает"; exit 2 ;;
esac
case "$WEB_PORT" in
  ''|*[!0-9]*) c_err "--web-port должен быть числом, получено: '$WEB_PORT'"; exit 2 ;;
esac

# ── 1. проверка системы ──────────────────────────────────────────────────────
step "1/9 Проверка системы"

if [ -r /etc/os-release ]; then
  # shellcheck disable=SC1091
  . /etc/os-release
  c_ok "ОС: ${PRETTY_NAME:-$NAME}"
  case "${ID:-}" in
    ubuntu|debian) ;;
    *) c_warn "поддерживаются Ubuntu/Debian; на ${ID:-неизвестной ОС} установка пакетов может не сработать" ;;
  esac
else
  c_warn "нет /etc/os-release — не могу определить ОС"
fi

ARCH="$(uname -m)"
c_ok "архитектура: $ARCH"

ghcr_code="000"
if command -v curl >/dev/null 2>&1; then
  ghcr_code="$(curl -s -o /dev/null -m 5 -w '%{http_code}' https://ghcr.io/v2/ 2>/dev/null || echo 000)"
fi
case "$ghcr_code" in
  200|401) c_ok "интернет и ghcr.io доступны (HTTP $ghcr_code — для реестра это норма)" ;;
  *)       c_warn "ghcr.io не ответил (код $ghcr_code) — pull образов может не пройти" ;;
esac

FREE_MB=$(df -Pm / 2>/dev/null | awk 'NR==2{print $4}')
if [ -n "${FREE_MB:-}" ] && [ "$FREE_MB" -lt 3000 ] 2>/dev/null; then
  c_warn "на / свободно ${FREE_MB} МБ — для образов и БД лучше ≥ 3 ГБ"
else
  c_ok "диск: свободно ${FREE_MB:-?} МБ"
fi

# ── 2. пакеты ────────────────────────────────────────────────────────────────
step "2/9 Пакеты: git, curl, docker, compose v2"

missing=""
for c in git curl; do command -v "$c" >/dev/null 2>&1 || missing="$missing $c"; done

if ! command -v docker >/dev/null 2>&1; then
  missing="$missing docker"
elif ! docker compose version >/dev/null 2>&1; then
  missing="$missing compose"
fi

if [ -n "$missing" ]; then
  c_info "не хватает:$missing — устанавливаю"
  run_sh "$SUDO apt-get update -qq"
  run_sh "$SUDO env DEBIAN_FRONTEND=noninteractive apt-get install -y -qq ca-certificates curl git"
  if ! command -v docker >/dev/null 2>&1; then
    run_sh "$SUDO env DEBIAN_FRONTEND=noninteractive apt-get install -y -qq docker.io"
  fi
  if ! docker compose version >/dev/null 2>&1; then
    run_sh "$SUDO env DEBIAN_FRONTEND=noninteractive apt-get install -y -qq docker-compose-v2" \
      || run_sh "$SUDO env DEBIAN_FRONTEND=noninteractive apt-get install -y -qq docker-compose-plugin" \
      || c_warn "compose v2 не установился из пакетов"
  fi
else
  c_ok "всё на месте"
fi

if command -v docker >/dev/null 2>&1; then
  run_sh "$SUDO systemctl enable --now docker >/dev/null 2>&1 || true"
  if [ -n "$SUDO" ] || [ "$(id -un)" != "$DEPLOY_USER" ]; then
    run_sh "$SUDO usermod -aG docker '$DEPLOY_USER' >/dev/null 2>&1 || true"
  fi
  if ! command -v docker >/dev/null 2>&1 || ! docker compose version >/dev/null 2>&1; then
    c_err "docker/compose всё ещё недоступны — установите вручную и запустите скрипт снова"
    c_err "официальный способ: curl -fsSL https://get.docker.com | sh"
    exit 1
  fi
  # группа docker в текущей сессии ещё не применена — тогда идём через sudo
  if [ "$DRY" -eq 1 ]; then
    DOCKER="$SUDO docker"
  elif docker info >/dev/null 2>&1; then
    DOCKER="docker"
  else
    DOCKER="$SUDO docker"
    c_info "docker доступен только через sudo (перелогиньтесь, чтобы работать без него)"
  fi
  c_ok "docker: $($DOCKER --version 2>/dev/null | cut -d, -f1)"
  c_ok "compose: $($DOCKER compose version --short 2>/dev/null || echo v2)"
else
  DOCKER="docker"
fi

# ── 3. каталог и код ─────────────────────────────────────────────────────────
step "3/9 Каталог и код ($TARGET, ветка $BRANCH)"

run_sh "$SUDO mkdir -p '$TARGET'"
if [ "$(id -u)" -eq 0 ] && [ "$DEPLOY_USER" != "root" ]; then
  run_sh "chown -R '$DEPLOY_USER':'$DEPLOY_USER' '$TARGET'"
fi

if [ -d "$TARGET/.git" ]; then
  c_ok "репозиторий уже есть — обновляю"
  # shallow-клон ломает откат на старые sha
  run_sh "git -C '$TARGET' config --global --add safe.directory '$TARGET' 2>/dev/null || true"
  if [ "$DRY" -eq 0 ] && [ -f "$TARGET/.git/shallow" ]; then
    c_info "клон был shallow — добираю историю (нужна для отката)"
    run_sh "git -C '$TARGET' fetch --unshallow || true"
  fi
  run_sh "git -C '$TARGET' fetch --quiet origin"
  run_sh "git -C '$TARGET' reset --hard --quiet 'origin/$BRANCH'"
else
  c_info "клонирую $REPO_URL"
  run_sh "git clone --quiet --branch '$BRANCH' '$REPO_URL' '$TARGET'"
fi

if [ "$DRY" -eq 0 ]; then
  c_ok "HEAD: $(git -C "$TARGET" rev-parse --short HEAD) ($(git -C "$TARGET" log -1 --pretty=%s | cut -c1-50))"
  if [ ! -f "$TARGET/infra/docker-compose.prod.yml" ]; then
    c_err "в $TARGET нет infra/docker-compose.prod.yml — не та ветка?"; exit 1
  fi
fi

# ── 4. infra/.env ────────────────────────────────────────────────────────────
step "4/9 Настройки (infra/.env)"

ENV_FILE="$TARGET/infra/.env"
ENV_EXAMPLE="$TARGET/infra/.env.example"

gen_password() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 24 | tr -d '/+=' | cut -c1-24
  else
    LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24
  fi
}

if [ -f "$ENV_FILE" ] && [ "$FORCE" -eq 0 ]; then
  c_ok "infra/.env уже есть — не трогаю (перезаписать: --force)"
  if [ "$DRY" -eq 0 ]; then
    cur_host="$(grep -E '^APP_HOST=' "$ENV_FILE" | tail -1 | cut -d= -f2- || true)"
    if [ "$cur_host" != "$APP_HOST" ]; then
      c_warn "в .env APP_HOST='$cur_host', а передан '$APP_HOST' — правки не применены"
    fi
  fi
else
  if [ ! -f "$ENV_EXAMPLE" ]; then
    if [ "$DRY" -eq 1 ]; then
      c_warn "dry-run: $ENV_EXAMPLE появится после клонирования — команды ниже только печатаются"
    else
      c_err "нет $ENV_EXAMPLE — не тот каталог или не та ветка"; exit 1
    fi
  fi
  DB_PASS="$(gen_password)"
  c_info "создаю infra/.env (пароль БД сгенерирован)"
  run_sh "cp '$ENV_EXAMPLE' '$ENV_FILE'"
  run_sh "sed -i 's|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=$DB_PASS|' '$ENV_FILE'"
  run_sh "sed -i 's|^APP_HOST=.*|APP_HOST=$APP_HOST|' '$ENV_FILE'"
  if [ -f "$ENV_EXAMPLE" ] && grep -qE '^WEB_PORT=' "$ENV_EXAMPLE"; then
    run_sh "sed -i 's|^WEB_PORT=.*|WEB_PORT=$WEB_PORT|' '$ENV_FILE'"
  else
    run_sh "printf 'WEB_PORT=%s\\n' '$WEB_PORT' >> '$ENV_FILE'"
  fi
  run_sh "chmod 600 '$ENV_FILE'"
  if [ "$(id -u)" -eq 0 ] && [ "$DEPLOY_USER" != "root" ]; then
    run_sh "chown '$DEPLOY_USER':'$DEPLOY_USER' '$ENV_FILE'"
  fi
  c_warn "СОХРАНИТЕ пароль БД: $DB_PASS"
fi

# ── 5. известная ловушка healthcheck ─────────────────────────────────────────
step "5/9 Проверка healthcheck web"

COMPOSE="$TARGET/infra/docker-compose.prod.yml"
LOCAL_OVERRIDE="$TARGET/infra/docker-compose.local.yml"
compose_args="-f infra/docker-compose.prod.yml"
[ -f "$LOCAL_OVERRIDE" ] && compose_args="$compose_args -f infra/docker-compose.local.yml"

if [ "$DRY" -eq 0 ] && grep -q 'wget", "-qO-", "http://localhost/"' "$COMPOSE"; then
  c_warn "в compose healthcheck web использует http://localhost/ — в Alpine это часто IPv6 (::1),"
  c_warn "а nginx слушает только IPv4 → контейнер становится unhealthy и 'up --wait' падает."
  if [ ! -f "$LOCAL_OVERRIDE" ]; then
    c_info "создаю untracked override infra/docker-compose.local.yml (git reset его не тронет)"
    cat > "$LOCAL_OVERRIDE" <<'YAML'
# Локальный override: чинит healthcheck web (localhost → 127.0.0.1, + start_period).
# Файл не отслеживается git, поэтому CD-деплой с git reset --hard его не удалит.
services:
  web:
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://127.0.0.1/ >/dev/null || exit 1"]
      interval: 20s
      timeout: 5s
      retries: 5
      start_period: 10s
YAML
    [ "$(id -u)" -eq 0 ] && [ "$DEPLOY_USER" != "root" ] && chown "$DEPLOY_USER":"$DEPLOY_USER" "$LOCAL_OVERRIDE"
  fi
  c_warn "ПРАВИЛЬНОЕ решение — поправить healthcheck в репозитории (PR), иначе CD-деплой будет падать на 'up --wait'"
elif [ "$DRY" -eq 1 ]; then
  c_info "dry-run: проверка healthcheck пропущена (compose появится после клонирования)"
else
  c_ok "healthcheck не вызывает известной проблемы"
fi

# ── 6. образы ────────────────────────────────────────────────────────────────
step "6/9 Образы из GHCR"

if command -v docker >/dev/null 2>&1; then
  if $DOCKER manifest inspect ghcr.io/maks-march/memorandum-api:stable >/dev/null 2>&1; then
    c_ok "пакеты публичные — логин не нужен"
  else
    c_warn "не удалось проверить анонимный доступ; если пакеты приватные — нужен:"
    c_warn "  read -rs -p 'PAT: ' P; echo \"\$P\" | docker login ghcr.io -u maks-march --password-stdin"
  fi
elif [ "$DRY" -eq 1 ]; then
  c_info "dry-run: docker ещё не установлен — проверка образов пропущена"
fi

# ── 7. опционально: ufw, cron, ssh-ключ ──────────────────────────────────────
if [ "$SETUP_UFW" -eq 1 ]; then
  step "7/9 Файрвол (ufw)"
  run_sh "$SUDO ufw allow 22/tcp >/dev/null 2>&1 || true"
  run_sh "$SUDO ufw allow '$WEB_PORT'/tcp >/dev/null 2>&1 || true"
  run_sh "$SUDO ufw allow 443/tcp >/dev/null 2>&1 || true"
  run_sh "$SUDO ufw --force enable >/dev/null 2>&1 || true"
  c_ok "открыты 22, $WEB_PORT, 443"
else
  step "7/9 Файрвол (ufw) — пропущен (--ufw)"
fi

if [ -n "$SSH_KEY_FILE" ]; then
  if [ -f "$SSH_KEY_FILE" ]; then
    c_info "добавляю ключ $SSH_KEY_FILE в authorized_keys пользователя $DEPLOY_USER"
    run_sh "install -d -m 700 -o '$DEPLOY_USER' -g '$DEPLOY_USER' ~'$DEPLOY_USER'/.ssh 2>/dev/null || true"
    run_sh "grep -qxF \"\$(cat '$SSH_KEY_FILE')\" ~'$DEPLOY_USER'/.ssh/authorized_keys 2>/dev/null || cat '$SSH_KEY_FILE' >> ~'$DEPLOY_USER'/.ssh/authorized_keys"
    run_sh "chmod 600 ~'$DEPLOY_USER'/.ssh/authorized_keys && chown '$DEPLOY_USER':'$DEPLOY_USER' ~'$DEPLOY_USER'/.ssh/authorized_keys"
    c_ok "ключ добавлен (для CD: DEPLOY_USER=$DEPLOY_USER)"
  else
    c_warn "файл ключа $SSH_KEY_FILE не найден — пропускаю"
  fi
fi

if [ "$SETUP_CRON" -eq 1 ]; then
  c_info "ставлю бэкап БД в cron (03:00)"
  CRON_LINE="0 3 * * * cd $TARGET && docker compose -f infra/docker-compose.prod.yml --env-file infra/.env --profile tools run --rm backup >/dev/null 2>&1"
  run_sh "( crontab -l 2>/dev/null | grep -vF 'profile tools run --rm backup' ; echo '$CRON_LINE' ) | crontab -"
  c_ok "cron настроен"
fi

# ── 8. запуск ────────────────────────────────────────────────────────────────
if [ "$DO_UP" -eq 0 ]; then
  step "8/9 Запуск — пропущен (--skip-up)"
else
  step "8/9 Запуск стека"

  if [ "$DRY" -eq 1 ]; then
    printf '   \033[90m$ cd %s && %s compose %s --env-file infra/.env pull\033[0m\n' "$TARGET" "$DOCKER" "$compose_args"
    printf '   \033[90m$ %s compose %s --env-file infra/.env up -d --remove-orphans\033[0m\n' "$DOCKER" "$compose_args"
  else
    cd "$TARGET"
    run_sh "$DOCKER compose $compose_args --env-file infra/.env pull"
    # без --wait: сначала поднимаем, потом ждём health сами (--wait падает на unhealthy web)
    run_sh "$DOCKER compose $compose_args --env-file infra/.env up -d --remove-orphans"

    BASE="http://127.0.0.1:${WEB_PORT}"
    printf '   ждём /api/health'
    up=0
    for _ in $(seq 1 60); do
      if curl -fsS -m 3 -o /dev/null "$BASE/api/health" 2>/dev/null; then up=1; break; fi
      printf '.'; sleep 2
    done
    printf '\n'
    if [ "$up" -eq 1 ]; then c_ok "api отвечает"; else c_err "api не ответил за 120 с — смотрите: docker compose logs api"; fi

    # ── 9. smoke ─────────────────────────────────────────────────────────────
    step "9/9 Smoke"

    fails=0
    check() { # url, ожидание, описание
      code="$(curl -s -o /dev/null -w '%{http_code}' -m 5 "$1" 2>/dev/null || echo 000)"
      if [ "$code" = "$2" ]; then c_ok "$3 ($code)"; else c_err "$3: ожидали $2, получили $code"; fails=$((fails+1)); fi
    }
    check "$BASE/" 200 "статика /"
    check "$BASE/mock/api/health" 404 "mock закрыт"
    check "$BASE/api/health" 200 "health"

    health="$(curl -fsS -m 5 "$BASE/api/health" 2>/dev/null || true)"
    printf '%s\n' "$health" | grep -q '"environment":"Production"' && c_ok "окружение Production" || { c_err "в health не Production"; fails=$((fails+1)); }
    printf '%s\n' "$health" | grep -q '"version":"[0-9][^"]*+' && c_ok "версия сборки есть (sha)" || c_warn "версия без sha — образ собран локально?"

    if curl -sI -m 5 "$BASE/" 2>/dev/null | grep -qi 'content-security-policy'; then
      c_ok "security-заголовки на /"
    else
      c_warn "нет CSP на / — проверьте nginx-сниппет"
    fi

    if [ "$fails" -eq 0 ]; then
      c_ok "smoke пройден"
    else
      c_err "провалов: $fails — логи: docker compose $compose_args --env-file infra/.env logs --tail 60"
    fi
  fi
fi

# ── итог ─────────────────────────────────────────────────────────────────────
step "Готово"

cat <<EOF

  Приложение:   http://${APP_HOST}$( [ "$WEB_PORT" != "80" ] && printf ':%s' "$WEB_PORT" )
  Health:       http://${APP_HOST}$( [ "$WEB_PORT" != "80" ] && printf ':%s' "$WEB_PORT" )/api/health
  Каталог:      ${TARGET}
  Ветка:        ${BRANCH}

  Что положить в GitHub → Settings → Environments → production → Secrets:
    DEPLOY_HOST = ${APP_HOST}
    DEPLOY_USER = ${DEPLOY_USER}
    SSH_KEY     = приватный ключ этого пользователя (целиком)
    GHCR_USER / GHCR_TOKEN — не нужны: пакеты публичные

  Затем: Actions → Deploy → Run workflow
    path = ${TARGET}
    sha  = (пусто — возьмётся SHA запуска)

  Полезное:
    cd ${TARGET} && docker compose ${compose_args} --env-file infra/.env ps
    cd ${TARGET} && docker compose ${compose_args} --env-file infra/.env logs -f api
    бэкап БД:  docker compose ${compose_args} --env-file infra/.env --profile tools run --rm backup

EOF

if [ "$DRY" -eq 1 ]; then
  c_warn "это был --dry-run: ничего не выполнено"
fi
