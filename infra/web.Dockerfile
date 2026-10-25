# ─────────────────────────────────────────────────────────────────────────────
#  PROD-образ фронта: сборка Vite → раздача nginx + прокси /api на api:8080
#  docker build -f infra/web.Dockerfile -t memorandum-web .
#
#  APP_VERSION — короткий git sha, попадает в бандл как VITE_APP_VERSION и
#  показывается бейджем: smoke видит, какая статика реально раздаётся.
# ─────────────────────────────────────────────────────────────────────────────
FROM node:22-alpine AS build
ARG APP_VERSION=local
ENV VITE_APP_VERSION=${APP_VERSION}
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM nginx:1.27-alpine
RUN rm -f /etc/nginx/conf.d/default.conf
COPY infra/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY infra/nginx/snippets /etc/nginx/snippets
COPY --from=build /app/dist /usr/share/nginx/html
ENV APP_HOST=localhost
EXPOSE 80 443
