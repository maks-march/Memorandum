#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
#  Memorandum — миграции EF Core.
#
#  Одна точка входа вместо запоминания проекта, стартового проекта и каталога
#  вывода: dotnet-ef требует, чтобы стартовым проектом была Infrastructure
#  (в Api нет Microsoft.EntityFrameworkCore.Design), а миграции лежат
#  в Persistence/Migrations.
#
#  Использование:
#    ./backend/migrations.sh add ДобавитьПроекты     # новая миграция
#    ./backend/migrations.sh list                    # какие есть и какие применены
#    ./backend/migrations.sh script                  # SQL для наката (в stdout)
#    ./backend/migrations.sh script --idempotent     # SQL, безопасный для повтора
#    ./backend/migrations.sh script ОТ..ДО --output file.sql
#    ./backend/migrations.sh apply                   # накатить на БД из конфигурации
#    ./backend/migrations.sh remove                  # откатить последнюю НЕприменённую
#
#  dotnet-ef ставится локально в backend/.tools (в .gitignore), поэтому
#  глобальные тулы и права на машину не нужны.
#
#  list и apply ходят в настоящую БД, поэтому им нужна строка подключения
#  из appsettings или из переменной CONNECTION. add/script/remove работают
#  офлайн: им хватает строки из AppDbContextFactory.
#
#  Переменные:
#    DOTNET — путь к dotnet, по умолчанию из PATH
#    EF_VERSION — версия dotnet-ef, по умолчанию 10.0.15
#    CONNECTION — строка подключения для list/apply (иначе из appsettings)
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA="$ROOT/src/Memorandum.Infrastructure"
MIGRATIONS="$INFRA/Persistence/Migrations"
TOOLS="$ROOT/.tools"
DOTNET="${DOTNET:-dotnet}"
EF_VERSION="${EF_VERSION:-10.0.15}"
EF="$TOOLS/dotnet-ef"

die() {
  echo "Ошибка: $*" >&2
  exit 1
}

usage() {
  sed -n '2,32p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

# dotnet-ef ставится один раз и переиспользуется; версия фиксируется, чтобы
# миграции не зависели от того, что оказалось в PATH у разработчика.
ensure_ef() {
  command -v "$DOTNET" >/dev/null 2>&1 || die "dotnet не найден. Установите SDK 10 или задайте DOTNET=/путь/к/dotnet"

  if [[ -x "$EF" ]]; then
    return
  fi

  echo "Ставлю dotnet-ef $EF_VERSION в backend/.tools …"
  "$DOTNET" tool install dotnet-ef --tool-path "$TOOLS" --version "$EF_VERSION" >/dev/null
}

run_ef() {
  "$EF" "$@" --project "$INFRA" --startup-project "$INFRA"
}

# Подключение к живой БД. Без него «НЕ применена» и «применить» неотличимы
# от «база недоступна», поэтому просим строку явно, а не гадаем.
require_connection() {
  if [[ -z "${CONNECTION:-}" ]]; then
    die "для «$1» нужна живая БД: задайте CONNECTION=\"Host=...;Database=...;Username=...;Password=...\""
  fi
}

command="${1:-}"
[[ -n "$command" ]] || { usage; exit 1; }
shift

case "$command" in
  add)
    name="${1:-}"
    [[ -n "$name" ]] || die "укажите имя миграции: ./backend/migrations.sh add AddProjects"

    # Имя становится частью класса и файла — кириллица и пробелы там не нужны.
    [[ "$name" =~ ^[A-Za-z][A-Za-z0-9]*$ ]] \
      || die "имя миграции — латиница и цифры, с заглавной буквы (например AddProjects)"

    ensure_ef
    run_ef migrations add "$name" --output-dir Persistence/Migrations
    echo
    echo "Готово. Проверьте $MIGRATIONS — файлы нужно закоммитить."
    ;;

  list)
    require_connection list
    ensure_ef
    run_ef migrations list --connection "$CONNECTION"
    ;;

  script)
    ensure_ef
    run_ef migrations script "$@"
    ;;

  apply)
    require_connection apply
    ensure_ef
    run_ef database update --connection "$CONNECTION"
    ;;

  remove)
    # --force: последняя миграция обычно ещё не применена, а dotnet-ef без
    # этого флага пытается достучаться до БД, чтобы проверить это.
    ensure_ef
    run_ef migrations remove --force
    ;;

  -h | --help | help)
    usage
    ;;

  *)
    die "неизвестная команда «$command». Доступно: add, list, script, apply, remove"
    ;;
esac
