# 02. REST API

Базовый путь `/api`, версия — **сегмент URL**: `/api/v1/...`. Формат — JSON, UTF-8. Общие соглашения:

- **Версия API:** `/api/v1/system/info`. Несуществующий сегмент (`/api/v9/...`) не маршрутизируется → **404** + `ProblemDetails`; путь без сегмента (`/api/system/info`) — тоже 404. В ответе приходит `api-supported-versions: 1.0`.
- **Заголовок `X-Api-Version` не используется** и игнорируется: healthcheck, мониторинг и старые клиенты со своим заголовком получают обычный ответ, а не 400.
- **Нейтральные эндпоинты** (`/api/health`, `/mock/api/health`) помечены `[ApiVersionNeutral]` — у них нет сегмента версии, они одинаковы для всех версий.
- **Кэширование:** версия в URL — часть ключа кэша nginx/CDN, поэтому ответы разных версий не пересекаются.
- **Документация:** OpenAPI публикуется по документу на версию — `/openapi/v1.json`; Scalar — `/scalar/v1`.
- **Ошибки:** любой ответ без тела (400/404) и любое необработанное исключение (500) отдаются как `application/problem+json` с `traceId`.

- **Auth:** cookie-сессия (`HttpOnly`, `Secure`, `SameSite=Lax`) + antiforgery-заголовок `X-XSRF-TOKEN` для мутаций.
- **Роли:** `SA` = service_admin, `OA` = org_admin, `ED` = editor, `VW` = viewer.
- **Изоляция:** всё ниже `/projects`, `/documents` неявно ограничено `org_id` пользователя. Чужой объект → **404** (не 403, чтобы не палить существование).
- **Валидация:** `ProblemDetails` + массив `errors[]` по полям (`application/problem+json`).
- **Пагинация:** `?page=1&pageSize=20&sort=-updatedAt`, ответ `{ items, total, page, pageSize }`.
- **Конкурентность:** документ отдаёт `ETag: "<xmin>"`; мутации документа требуют `If-Match` → `412` при несовпадении, `409` если нужен force.
- **Идемпотентность:** создающие POST принимают `Idempotency-Key`.

## Окружение и флаги

| Что | Development | Production |
|---|---|---|
| `Database:Provider` | `Sqlite` | только `Postgres` — иначе приложение не стартует |
| `ConnectionStrings:Postgres` | не требуется | обязательна, иначе приложение не стартует |
| `Mock:Enabled` | `true` (`/mock/api/health` доступен) | `true` запрещён — приложение не стартует |
| `AllowedHosts` | допускается `*` | обязателен список доменов; `*` запрещён |
| `OpenApi:Enabled` | включено всегда | по умолчанию выключено, включается флагом (в лог пишется предупреждение) |
| `Proxy:TrustedNetworks` | не задаётся; по умолчанию `172.16.0.0/12` (пул docker) | обязателен явный CIDR — `Proxy__TrustedNetworks__0=172.28.0.0/16`, иначе приложение не стартует |

Проверки прод-конфигурации выполняются на старте (fail-fast) и покрыты тестами
`EnvironmentGuardsTests` — приложение не поднимется с dev-настройками.

**Доверие к прокси.** `X-Forwarded-For`/`X-Forwarded-Proto` принимаются только от адресов из
`Proxy:TrustedNetworks` — это docker-сеть, где живёт nginx. Раньше список доверенных сетей
очищался полностью, то есть заголовки принимались от кого угодно; теперь подменить реальный IP
снаружи нельзя. При изменении подсети в `docker-compose.prod.yml` (блок `networks.default.ipam`)
нужно поправить и `Proxy__TrustedNetworks__0` в сервисе `api`.

**CORS** настроен только для Development (`http://localhost:5173`), чтобы фронтендер мог ходить
на API напрямую, минуя Vite-прокси. В Production CORS не включается: фронт и API отдаёт один
nginx с одного origin.

## 1. Аутентификация и профиль

> Разделы 1–9 описывают **целевой контракт** и ещё не реализованы. Работает сейчас только
> служебная часть — см. §10.

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| POST | `/auth/register` | — | `{email, password, fullName, orgName?}` → создаёт пользователя (и организацию, где он `OA`) |
| POST | `/auth/login` | — | `{email, password}` → 200 + cookie; 401; 429 при брутфорсе |
| POST | `/auth/logout` | ☑ | гасит cookie |
| POST | `/auth/forgot-password` | — | `{email}` → всегда 204 (без раскрытия) |
| POST | `/auth/reset-password` | — | `{token, newPassword}` |
| GET | `/me` | ☑ | `{id, email, fullName, position, role, org:{id,name}}` |
| PATCH | `/me` | ☑ | ФИО, должность, телефон |
| POST | `/me/password` | ☑ | `{currentPassword, newPassword}` |

## 2. Организации и сотрудники

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| GET | `/organizations/current` | ☑ | своя организация (реквизиты: ИНН, КПП, ОГРН, адрес) |
| PATCH | `/organizations/current` | OA, SA | редактирование реквизитов |
| GET | `/organizations` | SA | список всех организаций (пагинация, поиск) |
| POST | `/organizations` | SA, ☑ | создать организацию |
| PATCH | `/organizations/{id}` | SA | реквизиты / деактивация |
| GET | `/organizations/current/members` | OA, ED(read) | сотрудники: `{id, fullName, email, role, isActive, lastLoginAt}` |
| POST | `/organizations/current/members` | OA | добавить существующего пользователя / пригласить |
| PATCH | `/organizations/current/members/{userId}` | OA | сменить роль, заблокировать |
| DELETE | `/organizations/current/members/{userId}` | OA | убрать из организации (`org_id = null`) |
| GET | `/invitations` | OA | список приглашений (pending) |
| POST | `/invitations` | OA | `{email, role}` → 201 + token-ссылка |
| POST | `/invitations/{token}/accept` | — | регистрация по приглашению |
| DELETE | `/invitations/{id}` | OA | отозвать |

## 3. Справочники и схемы

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| GET | `/dictionaries/regions` | ☑ | субъекты РФ `[{code,name}]` |
| GET | `/dictionaries/oktmo?q=&region=` | ☑ | автокомплит ОКТМО (limit 20) |
| GET | `/dictionaries/units` | ☑ | единицы измерения |
| GET | `/dictionaries/tep-catalog?objectKind=` | ☑ | типовые ТЭП для подсказки |
| GET | `/schema-versions` | ☑ | активные версии: `[{code:'01.07', title, effectiveFrom, isDefault}]` |
| GET | `/schema-versions/{code}/form-manifest` | ☑ | **manifest полей**: `[{fieldId, section, label, kind, required, dictionary, format, hint, group}]`; `ETag` |
| GET | `/schema-versions/{code}` | SA | детали версии + список файлов комплекта |
| POST | `/admin/schema-versions` | SA | `multipart: zip + {code, title, publishedAt, effectiveFrom, isActive}` → распаковка, sha256, компиляция `XmlSchemaSet` с `XmlResolver=null`; 422 если схема не компилируется |
| PATCH | `/admin/schema-versions/{code}` | SA | `isActive`, `isDefault`, даты |
| DELETE | `/admin/schema-versions/{code}` | SA | запрещено, если есть документы |

## 4. Проекты

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| GET | `/projects?q=&archived=&responsibleId=&page=` | ☑ | список проектов организации |
| POST | `/projects` | OA, ED | `{name, objectName, objectKind, addressText, workType, responsibleUserId?, code?}` |
| GET | `/projects/{id}` | ☑ | карточка + счётчики документов |
| PATCH | `/projects/{id}` | OA, ED | правка полей |
| DELETE | `/projects/{id}` | OA | 409 если есть документы (только архив) |
| POST | `/projects/{id}/archive` \| `/unarchive` | OA, ED | архив проекта |

## 5. Документы (пояснительные записки)

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| GET | `/documents?q=&projectId=&status=&schemaCode=&authorId=&archived=&sort=&page=` | ☑ | единый список + фильтры ТЗ §5.16 |
| POST | `/projects/{projectId}/documents` | OA, ED | `{title, schemaCode}` → 201, `data={}`, статус `draft` |
| GET | `/documents/{id}` | ☑ | `{id, title, status, schemaCode, data, completion, updatedAt}` + `ETag` |
| PATCH | `/documents/{id}` | OA, ED | заголовок, ответственный (метаданные) |
| DELETE | `/documents/{id}` | OA | soft-delete/архив |
| **PATCH** | **`/documents/{id}/sections/{section}`** | OA, ED | **автосохранение**: `{payload}` для одной секции (`object`, `participants`, `specialists`, `basis`, `tep`, `composition`, `normatives`, `surveys`, `climate`, `extra`). `If-Match` → 412/409 |
| POST | `/documents/{id}/archive` \| `/unarchive` | OA, ED | архив/восстановление |
| GET | `/documents/{id}/completion` | ☑ | прогресс по секциям `{section:{filled,total,required}}` |

## 6. Проверка, XML, предпросмотр

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| POST | `/documents/{id}/validate` | OA, ED | доменные правила + генерация + **XSD-валидация**. Ответ: `{ok, checkedAt, errors:[{fieldId, section, code, severity, message, xmlPath}], counts:{errors,warnings}}`. Меняет статус: `verified` / `has_errors`; пишет `checks` |
| GET | `/documents/{id}/checks?limit=10` | ☑ | история проверок |
| POST | `/documents/{id}/xml` | OA, ED | сгенерировать и сохранить артефакт (без смены статуса) → `{sha256, sizeBytes, schemaCode, valid}` |
| **GET** | **`/documents/{id}/xml/download`** | ☑ | `application/xml`, `Content-Disposition`; **409 `not_verified`**, если последняя проверка неуспешна |
| GET | `/documents/{id}/xml` | ☑ | просмотр XML (text/xml, read-only, для вкладки «XML») |
| GET | `/documents/{id}/preview` | ☑ | JSON-модель для предпросмотра `{header, object, participants, tep[], composition[], ...}` |
| GET | `/documents/{id}/preview.html` | ☑ | HTML-рендер (XSL из комплекта, если есть; иначе серверный шаблон) |

## 7. Импорт

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| POST | `/projects/{projectId}/documents/import` | OA, ED | `multipart/form-data: file` (+ `title?`) → `{documentId, detectedSchema, importedFields, warnings:[{path, reason}], originalFileId}`. 415 неизвестная версия → понятное сообщение |
| GET | `/documents/{id}/imports` | ☑ | история импортов |
| GET | `/documents/{id}/imports/{importId}/original` | ☑ | скачать исходный XML «как есть» |

## 8. Версии документа

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| GET | `/documents/{id}/versions` | ☑ | `[{number, createdAt, createdBy, comment, isAuto, status}]` |
| POST | `/documents/{id}/versions` | OA, ED | `{comment?}` → снапшот `data`, `number+1` |
| GET | `/documents/{id}/versions/{n}` | ☑ | снапшот: `{data, status, createdAt}` (read-only режим редактора) |
| POST | `/documents/{id}/versions/{n}/restore` | OA, ED | восстановить → авто-снапшот текущего + `data` из n |
| GET | `/documents/{id}/versions/diff?from=2&to=5` | ☑ | `[{fieldId, label, before, after}]` (плоский дифф по каталогу) |

## 9. Админка сервиса

| Метод | Путь | Роли | Описание |
|---|---|---|---|
| GET | `/admin/users?q=&orgId=&role=&isActive=` | SA (+OA: только своя орг.) | список пользователей |
| POST | `/admin/users` | SA | создать пользователя (без саморегистрации) |
| PATCH | `/admin/users/{id}` | SA, OA | роль, активность, сброс пароля |
| DELETE | `/admin/users/{id}` | SA | блокировка (не удаление) |
| GET | `/admin/organizations` | SA | список + счётчики (пользователи, проекты, документы) |
| GET | `/admin/documents?orgId=&status=&schemaCode=` | SA | документы без содержимого — только метаданные |
| GET | `/admin/errors?level=&from=&to=` | SA | технические ошибки (`app_errors`) |
| GET | `/admin/audit?entity=&entityId=&userId=` | SA, OA | журнал действий |
| GET | `/admin/stats` | SA | сводка: пользователи, документы по статусам, версии схем |

## 10. Служебные

Реализовано сейчас:

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/health` | диагностика: окружение, провайдер БД, версия сборки, имя контейнера. Версия API не требуется (`[ApiVersionNeutral]`) |
| GET | `/api/v1/system/info` | поддерживаемые версии API и версия сборки; первый версионированный эндпоинт |
| GET | `/mock/api/health` | заглушка для фронтенда: `?delay=0..10000`, `?status=200\|401\|404\|500`. Только когда `Mock:Enabled=true` (в Development) |
| GET | `/openapi/v1.json` | спецификация OpenAPI, по документу на версию |
| GET | `/scalar/v1` | интерактивная документация (Scalar) |

Планируется позже (вместе с EF Core):

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/health/deep` | проверка БД + компиляция `XmlSchemaSet` для активных версий XSD |

Разделения liveness/readiness не будет: отдельные `/healthz` и `/ready` не планируются — одна
проба `/api/health` обслуживает healthcheck'и compose и nginx. Когда появится EF Core, проверка БД
добавится внутрь этого же эндпоинта.

## 11. Коды ответов (сводка)

| Код | Когда |
|---|---|
| 200/201/204 | успех |
| 400 | невалидный DTO (`errors[]` по полям) |
| 401 | нет/истекла cookie |
| 403 | роль не позволяет (для своего же ресурса) |
| 404 | нет ресурса **или чужой org** |
| 409 | конфликт состояния: XML не проверен, документ не в статусе, нужен force |
| 412 | `If-Match` устарел |
| 413/415/422 | слишком большой файл / неизвестная версия схемы / схема не компилируется |
| 429 | rate limit (login, импорт) |
| 500 | ошибка → `app_errors` + `traceId` |
