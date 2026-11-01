#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
#  Перезагрузка nginx после продления сертификата.
#
#  certbot при renew идемпотентен: если сертификат не менялся, он ничего не делает
#  и deploy-hook не срабатывает, флага нет — скрипт выходит сразу. Когда обновление
#  реально произошло, hook создаёт /var/www/certbot/.reload, и только тогда nginx
#  перечитывает конфиг — без слепого reload каждые 12 часов.
#
#  Ставится в cron хоста автоматически при `prod-up.sh --tls`:
#    0 4 * * * cd /opt/memorandum && ./infra/reload-certs.sh >> /var/log/memorandum-certs.log 2>&1
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"

if [ ! -f "$ENV_FILE" ]; then
  echo "нет $ENV_FILE — нечего перезагружать" >&2
  exit 1
fi

APP_HOST="$(grep -E '^APP_HOST=' "$ENV_FILE" | tail -1 | cut -d= -f2- || true)"

COMPOSE=(docker compose -f "$SCRIPT_DIR/docker-compose.prod.yml"
         -f "$SCRIPT_DIR/docker-compose.tls.yml" --env-file "$ENV_FILE")

# флаг появился в томе у certbot — значит renew что-то обновил
if "${COMPOSE[@]}" exec -T certbot sh -c 'test -f /var/www/certbot/.reload' 2>/dev/null; then
  echo "$(date -u +%FT%TZ) сертификат обновлён — перезагружаю nginx"
  "${COMPOSE[@]}" exec -T web nginx -s reload
  "${COMPOSE[@]}" exec -T certbot rm -f /var/www/certbot/.reload
  echo "$(date -u +%FT%TZ) готово"
fi

# Диагностика: молчаливое падение продления — самая частая причина «внезапно
# истёкшего» сертификата. Раз в сутки проверяем остаток срока и шумим в cron-лог.
if [ -n "${APP_HOST:-}" ] && command -v openssl >/dev/null 2>&1 \
   && "${COMPOSE[@]}" ps --services --filter status=running 2>/dev/null | grep -qx certbot; then
  if "${COMPOSE[@]}" exec -T certbot cat "/etc/letsencrypt/live/$APP_HOST/fullchain.pem" 2>/dev/null \
       | openssl x509 -noout -checkend 259200 >/dev/null 2>&1; then
    echo "$(date -u +%FT%TZ) сертификат $APP_HOST действителен ещё ≥ 3 дней"
  else
    echo "$(date -u +%FT%TZ) ВНИМАНИЕ: сертификат $APP_HOST отсутствует или истекает в ближайшие 3 дня" >&2
    exit 1
  fi
fi
