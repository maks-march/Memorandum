# ─────────────────────────────────────────────────────────────────────────────
#  PROD-образ API. Контекст сборки — корень репозитория:
#  docker build -f infra/api.Dockerfile -t memorandum-api .
#
#  SOURCE_REVISION — короткий git sha. Попадает в AssemblyInformationalVersion,
#  его отдаёт /api/health в поле version: по нему smoke подтверждает, что на стенде
#  именно эта сборка. При локальной сборке остаётся "local".
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SOURCE_REVISION=local
WORKDIR /src
COPY backend/ backend/
RUN dotnet restore backend/src/Memorandum.Api/Memorandum.Api.csproj
RUN dotnet publish backend/src/Memorandum.Api/Memorandum.Api.csproj -c Release -o /app --no-restore \
    -p:SourceRevisionId=${SOURCE_REVISION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_NOLOGO=true
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Memorandum.Api.dll"]
