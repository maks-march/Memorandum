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
#    sudo ./infra/prod-up.sh --app-host 203.0.113.10
#    sudo ./infra/prod-up.sh --app-host memorandum.example.ru --web-port 80 --ufw --cron
#    sudo ./infra/prod-up.sh --app-host 203.0.113.10 --dry-run    # только показать
#
#  Флаги:
#    --app-host HOST     домен или IP сервера (обязательно; попадает в AllowedHosts)
#    --web-port N        порт наружу, по умолчанию 80
#    --path DIR          каталог клона, по умолчанию /opt/memorandum
#    --branch BR         ветка, по умолчанию main
#    --user USER         владелец клона и деплой-пользователь (по умолчанию — текущий)
#    --ssh-key FILE      добавить публичный ключ в authorized_keys этого пользователя
#    --tls               включить HTTPS: certbot + редирект 80 → 443, состояние в TLS_ENABLED
#                        (reload-certs.sh при этом ставится в cron автоматически)
#    --local             собрать образы из этого checkout (IMAGE_TAG=local) вместо GHCR
#    --ufw               настроить ufw: 22, WEB_PORT и HTTPS_PORT
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
TLS=0
LOCAL_BUILD=0
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
    --tls)      TLS=1; shift ;;
    --local)    LOCAL_BUILD=1; shift ;;
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
      # иначе smoke пойдёт на новый хост, а nginx останется со старым server_name,
      # и провал будет выглядеть как «api не отвечает»
      c_err "в $ENV_FILE APP_HOST='$cur_host', а передан '$APP_HOST' — правки не применены"
      c_err "поправьте APP_HOST в infra/.env и повторите (--force перезапишет файл и сменит пароль БД)"
      exit 2
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

# ── 5. режим TLS ─────────────────────────────────────────────────────────────
step "5/9 Режим TLS"

# состояние читаем из infra/.env: тот же флаг использует CD, иначе деплой
# по кнопке пересоздаст web без TLS-оверрайда и сайт молча уйдёт на HTTP
current_tls="$(grep -E '^TLS_ENABLED=' "$ENV_FILE" 2>/dev/null | tail -1 | cut -d= -f2- || true)"
if [ -z "$current_tls" ]; then
  current_tls=0
  run_sh "printf 'TLS_ENABLED=0\\n' >> '$ENV_FILE'"
fi

# порт публикации 443: попадает в редирект 80 → 443 и в smoke
HTTPS_PORT="$(grep -E '^HTTPS_PORT=' "$ENV_FILE" 2>/dev/null | tail -1 | cut -d= -f2- || true)"
HTTPS_PORT="${HTTPS_PORT:-443}"

if [ "$TLS" -eq 1 ]; then
  case "$APP_HOST" in
    localhost|127.0.0.1|*[!a-zA-Z0-9.-]*)
      c_err "--tls: APP_HOST='$APP_HOST' не годится — нужен домен или публичный IP"; exit 2 ;;
  esac
  # 443 должен быть свободен: его займёт web после выпуска сертификата
  if [ "$DRY" -eq 0 ] && [ "$current_tls" != "1" ] \
     && { ss -ltn 2>/dev/null || netstat -ltn 2>/dev/null || true; } | grep -qE "[:.]${HTTPS_PORT}[[:space:]]"; then
    c_err "порт $HTTPS_PORT уже занят — освободите его и повторите"; exit 2
  fi
  if [ "$current_tls" = "1" ]; then c_ok "TLS уже включён"; else c_info "включаю TLS (TLS_ENABLED=1)"; fi
elif [ "$current_tls" = "1" ]; then
  c_ok "TLS включён ранее (TLS_ENABLED=1) — оставляю"
  TLS=1
else
  c_ok "HTTP-режим (TLS_ENABLED=0)"
fi

# ── 6. образы ────────────────────────────────────────────────────────────────
# IMAGE_TAG=local или --local — собираем из этого checkout, иначе тянем GHCR
image_tag="$(grep -E '^IMAGE_TAG=' "$ENV_FILE" 2>/dev/null | tail -1 | cut -d= -f2- || true)"
if [ "$LOCAL_BUILD" -eq 1 ] || [ "$image_tag" = "local" ]; then
  LOCAL_BUILD=1
  step "6/9 Образы — локальная сборка"
  if [ "$DRY" -eq 1 ]; then
    c_info "dry-run: версия будет взята из git после клонирования"
  else
    REV="$(git -C "$TARGET" rev-parse --short HEAD 2>/dev/null || echo local)"
    c_ok "соберём из HEAD $REV (IMAGE_TAG=local, SOURCE_REVISION=$REV)"
  fi
else
  LOCAL_BUILD=0
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
fi

# ── 7. опционально: ufw, cron, ssh-ключ ──────────────────────────────────────
if [ "$SETUP_UFW" -eq 1 ]; then
  step "7/9 Файрвол (ufw)"
  run_sh "$SUDO ufw allow 22/tcp >/dev/null 2>&1 || true"
  run_sh "$SUDO ufw allow '$WEB_PORT'/tcp >/dev/null 2>&1 || true"
  run_sh "$SUDO ufw allow '$HTTPS_PORT'/tcp >/dev/null 2>&1 || true"
  run_sh "$SUDO ufw --force enable >/dev/null 2>&1 || true"
  c_ok "открыты 22, $WEB_PORT, $HTTPS_PORT"
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
  c_ok "бэкап БД в cron (03:00)"
fi

# reload после продления сертификата нужен всегда, когда включён TLS: nginx
# держит сертификат в памяти и без reload продолжает отдавать старый, пока тот
# не истечёт. Поэтому ставим cron не по --cron, а по факту TLS-режима.
if [ "$TLS" -eq 1 ]; then
  CRON_TLS="0 4 * * * cd $TARGET && ./infra/reload-certs.sh >> /var/log/memorandum-certs.log 2>&1"
  run_sh "( crontab -l 2>/dev/null | grep -vF 'infra/reload-certs.sh' ; echo '$CRON_TLS' ) | crontab -"
  c_ok "перезагрузка nginx после продления сертификата (04:00)"
fi

# ── 8. запуск ────────────────────────────────────────────────────────────────
# TLS_ENABLED фиксируем только по факту пройденного smoke: иначе --skip-up
# записал бы TLS без сертификата, следующий Deploy поднял бы web с tls.yml,
# nginx не стартовал бы, и откат шёл бы с тем же оверрайдом — откатиться некуда
smoke_ok=0

BASE_FILES=(-f "$TARGET/infra/docker-compose.prod.yml")
TLS_FILES=(-f "$TARGET/infra/docker-compose.prod.yml" -f "$TARGET/infra/docker-compose.tls.yml")
base_args="${BASE_FILES[*]}"
tls_args="${TLS_FILES[*]}"
if [ "$TLS" -eq 1 ]; then compose_args="$tls_args"; else compose_args="$base_args"; fi

if [ "$DO_UP" -eq 0 ]; then
  step "8/9 Запуск — пропущен (--skip-up)"
else
  step "8/9 Запуск стека"

  # 1) HTTP-стек. nginx стартует с HTTP-конфигом: он отдаёт ACME-челлендж,
  #    а TLS-конфиг без сертификата не стартует вообще (nginx проверяет
  #    ssl_certificate при загрузке конфига). Поэтому 443 включается только
  #    после того, как сертификат реально появился.
  if [ "$DRY" -eq 1 ]; then
    if [ "$LOCAL_BUILD" -eq 1 ]; then
      printf '   \033[90m$ cd %s && %s compose %s --env-file infra/.env build\033[0m\n' "$TARGET" "$DOCKER" "$base_args"
    else
      printf '   \033[90m$ cd %s && %s compose %s --env-file infra/.env pull\033[0m\n' "$TARGET" "$DOCKER" "$base_args"
    fi
    printf '   \033[90m$ %s compose %s --env-file infra/.env up -d --remove-orphans db api web\033[0m\n' "$DOCKER" "$base_args"
    if [ "$TLS" -eq 1 ]; then
      printf '   \033[90m$ %s compose %s --env-file infra/.env up -d --no-deps certbot\033[0m\n' "$DOCKER" "$tls_args"
      printf '   \033[90m$ ... дождаться сертификата, затем %s compose %s up -d --force-recreate web\033[0m\n' "$DOCKER" "$tls_args"
    fi
  else
    cd "$TARGET"

    if [ "$LOCAL_BUILD" -eq 1 ]; then
      export IMAGE_TAG=local SOURCE_REVISION="$REV" APP_VERSION="$REV"
      run_sh "$DOCKER compose $base_args --env-file infra/.env build"
    else
      run_sh "$DOCKER compose $base_args --env-file infra/.env pull"
    fi

    run_sh "$DOCKER compose $base_args --env-file infra/.env up -d --remove-orphans db api web"

    if [ "$TLS" -eq 1 ]; then
      # --no-deps: не трогаем web, он сейчас должен отдавать ACME по HTTP
      run_sh "$DOCKER compose $tls_args --env-file infra/.env up -d --no-deps certbot"

      step "Ожидание сертификата"
      cert_ready=0
      for _ in $(seq 1 60); do
        # проверяем через сам контейнер: имя тома привязано к имени compose-
        # проекта из docker-compose.prod.yml, и при его смене жёстко заданное
        # memorandum_certs перестало бы совпадать
        # shellcheck disable=SC2086  # tls_args — список -f, нужен сплит
        if $DOCKER compose $tls_args --env-file infra/.env exec -T certbot \
             sh -c "test -f /etc/letsencrypt/live/$APP_HOST/fullchain.pem" >/dev/null 2>&1; then
          cert_ready=1; break
        fi
        printf '.'; sleep 5
      done
      printf '\n'

      if [ "$cert_ready" -eq 1 ]; then
        c_ok "сертификат для $APP_HOST получен"
        # теперь TLS-конфигу есть что грузить — включаем 443
        run_sh "$DOCKER compose $tls_args --env-file infra/.env up -d --force-recreate web"
      else
        c_err "сертификат не получен за 300 с — TLS НЕ включён"
        # shellcheck disable=SC2086  # tls_args — список -f, нужен сплит
        $DOCKER compose $tls_args --env-file infra/.env logs --tail 40 certbot || true
        if [ "$current_tls" = "1" ]; then
          c_warn "TLS был включён ранее, но сертификата нет: сайт останется на HTTP, пока certbot не выпустит новый"
          c_warn "TLS_ENABLED=1 остаётся, поэтому следующий Deploy снова поднимет web с TLS — дождитесь сертификата"
        else
          c_info "оставляю HTTP-режим (TLS_ENABLED=0)"
        fi
        # web уже работает с HTTP-конфигом — пересоздавать не нужно
        TLS=0
        compose_args="$base_args"
      fi
    fi

    if [ "$TLS" -eq 1 ]; then
      # ходим на localhost, но с настоящими Host/SNI: иначе nginx отдаст
      # default-сервер, и ни редирект, ни HSTS толком не проверить
      if [ "$HTTPS_PORT" = "443" ]; then BASE="https://$APP_HOST"; else BASE="https://$APP_HOST:$HTTPS_PORT"; fi
      CURL=(curl -fsSk -m 10 --resolve "$APP_HOST:$HTTPS_PORT:127.0.0.1")
    else
      BASE="http://127.0.0.1:${WEB_PORT}"
      CURL=(curl -fsS -m 10)
    fi

    printf '   ждём /api/health'
    up=0
    for _ in $(seq 1 60); do
      if "${CURL[@]}" -m 3 -o /dev/null "$BASE/api/health" 2>/dev/null; then up=1; break; fi
      printf '.'; sleep 2
    done
    printf '\n'
    if [ "$up" -eq 1 ]; then c_ok "api отвечает"; else c_err "api не ответил за 120 с — смотрите: docker compose logs api"; fi

    # ── 9. smoke ─────────────────────────────────────────────────────────────
    step "9/9 Smoke"

    fails=0
    check() { # url, ожидание, описание
      code="$("${CURL[@]}" -o /dev/null -w '%{http_code}' -m 5 "$1" 2>/dev/null || echo 000)"
      if [ "$code" = "$2" ]; then c_ok "$3 ($code)"; else c_err "$3: ожидали $2, получили $code"; fails=$((fails+1)); fi
    }
    check "$BASE/" 200 "статика /"
    check "$BASE/mock/api/health" 404 "mock закрыт"
    check "$BASE/api/health" 200 "health"

    health="$("${CURL[@]}" -m 5 "$BASE/api/health" 2>/dev/null || true)"
    if printf '%s\n' "$health" | grep -q '"environment":"Production"'; then
      c_ok "окружение Production"
    else
      c_err "в health не Production"; fails=$((fails+1))
    fi

    if printf '%s\n' "$health" | grep -q '"version":"[0-9][^"]*+'; then
      c_ok "версия сборки есть (sha)"
    else
      c_warn "версия без sha — образ собран локально?"
    fi

    if "${CURL[@]}" -I "$BASE/" 2>/dev/null | grep -qi 'content-security-policy'; then
      c_ok "security-заголовки на /"
    else
      c_warn "нет CSP на / — проверьте nginx-сниппет"
    fi

    if [ "$TLS" -eq 1 ]; then
      if "${CURL[@]}" -I "$BASE/" 2>/dev/null | grep -qi 'strict-transport-security'; then
        c_ok "HSTS отдаётся"
      else
        c_err "нет HSTS — подключите snippets/hsts.conf в HTTPS-блок"; fails=$((fails+1))
      fi
      # сертификат должен не только отдаваться, но и жить ещё хотя бы неделю:
      # иначе молчаливое падение продления станет аварией
      if command -v openssl >/dev/null 2>&1; then
        if openssl s_client -connect "127.0.0.1:${HTTPS_PORT}" -servername "$APP_HOST" </dev/null 2>/dev/null \
             | openssl x509 -noout -checkend 604800 >/dev/null 2>&1; then
          c_ok "сертификат действителен ещё ≥ 7 дней"
        else
          c_err "сертификат не читается или истекает в ближайшие 7 дней — проверьте certbot"; fails=$((fails+1))
        fi
      else
        c_warn "openssl не найден — срок действия сертификата не проверен"
      fi
      # редирект 80 → 443
      code="$(curl -s -o /dev/null -w '%{http_code}' -m 5 -H "Host: $APP_HOST" "http://127.0.0.1:${WEB_PORT}/" 2>/dev/null || echo 000)"
      if [ "$code" = "301" ]; then c_ok "HTTP → HTTPS редирект (301)"; else c_err "нет редиректа на HTTPS: получили $code"; fails=$((fails+1)); fi
    fi

    if [ "$fails" -eq 0 ]; then
      smoke_ok=1
      c_ok "smoke пройден"
    else
      c_err "провалов: $fails — логи: docker compose $compose_args --env-file infra/.env logs --tail 60"
    fi
  fi
fi

# ── фиксация TLS ─────────────────────────────────────────────────────────────
if [ "$TLS" -eq 1 ] && [ "$DO_UP" -eq 0 ]; then
  c_warn "TLS_ENABLED НЕ записан: --skip-up не поднимает стек и не проверяет TLS"
  TLS_ACTIVE="$current_tls"
elif [ "$TLS" -eq 1 ] && { [ "$DRY" -eq 1 ] || [ "$smoke_ok" -eq 1 ]; }; then
  if [ "$current_tls" != "1" ]; then
    run_sh "sed -i 's|^TLS_ENABLED=.*|TLS_ENABLED=1|' '$ENV_FILE'"
    if [ "$DRY" -eq 0 ]; then c_ok "TLS_ENABLED=1 записан в infra/.env"; fi
  fi
  TLS_ACTIVE=1
elif [ "$TLS" -eq 1 ]; then
  c_warn "smoke в TLS-режиме не прошёл — TLS_ENABLED не включён"
  TLS_ACTIVE="$current_tls"
else
  TLS_ACTIVE=0
fi

# ── итог ─────────────────────────────────────────────────────────────────────
step "Готово"

# какие файлы compose реально описывают запущенный стек — для подсказок ниже
if [ "$TLS_ACTIVE" = "1" ]; then
  compose_args="$tls_args"
else
  compose_args="$base_args"
fi

SCHEME="http"
if [ "$TLS_ACTIVE" = "1" ]; then
  SCHEME="https"
  if [ "$HTTPS_PORT" != "443" ]; then PORT_SUFFIX=":$HTTPS_PORT"; else PORT_SUFFIX=""; fi
else
  if [ "$WEB_PORT" != "80" ]; then PORT_SUFFIX=":$WEB_PORT"; else PORT_SUFFIX=""; fi
fi

cat <<EOF

  Приложение:   ${SCHEME}://${APP_HOST}${PORT_SUFFIX}
  Health:       ${SCHEME}://${APP_HOST}${PORT_SUFFIX}/api/health
  TLS:          $( [ "$TLS_ACTIVE" = "1" ] && echo "включён (TLS_ENABLED=1)" || echo "выключен (TLS_ENABLED=0)" )
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
