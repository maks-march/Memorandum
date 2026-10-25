#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
#  Перезагрузка nginx после продления сертификата.
#
#  certbot при renew идемпотентен: если сертификат не менялся, он ничего не делает
#  и deploy-hook не срабатывает, флага нет — скрипт выходит сразу. Когда обновление
#  реально произошло, hook создаёт /var/www/certbot/.reload, и только тогда nginx
#  перечитывает конфиг — без слепого reload каждые 12 часов.
#
#  Ставится в cron хоста (это делает `prod-up.sh --tls --cron`):
#    0 4 * * * cd /opt/memorandum && ./infra/reload-certs.sh >> /var/log/memorandum-certs.log 2>&1
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"

if [ ! -f "$ENV_FILE" ]; then
  echo "нет $ENV_FILE — нечего перезагружать" >&2
  exit 1
fi

COMPOSE=(docker compose -f "$SCRIPT_DIR/docker-compose.prod.yml"
         -f "$SCRIPT_DIR/docker-compose.tls.yml" --env-file "$ENV_FILE")

# флаг появился в томе у certbot — значит renew что-то обновил
if ! "${COMPOSE[@]}" exec -T certbot test -f /var/www/certbot/.reload 2>/dev/null; then
  exit 0
fi

echo "$(date -u +%FT%TZ) сертификат обновлён — перезагружаю nginx"
"${COMPOSE[@]}" exec -T web nginx -s reload
"${COMPOSE[@]}" exec -T certbot rm -f /var/www/certbot/.reload
echo "$(date -u +%FT%TZ) готово"
