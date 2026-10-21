#!/bin/sh
# Совмещённый рантайм: API (localhost:5000) + Blazor Web (0.0.0.0:$PORT) в одном контейнере.
set -eu

# Эфемерная БД: на каждый старт — чистая база (EnsureCreated + сид поднимут её заново).
mkdir -p /app/data
rm -f /app/data/callcalendar.db /app/data/callcalendar.db-shm /app/data/callcalendar.db-wal 2>/dev/null || true

# ВАЖНО: content root берётся из рабочей директории процесса, поэтому каждый
# процесс стартует из своего каталога (иначе статика Web отдаётся пустой).
cd /app/api
ConnectionStrings__sqlite="Data Source=/app/data/callcalendar.db" \
  dotnet CallCalendar.Api.dll &
api_pid=$!
trap 'kill "$api_pid" 2>/dev/null || true' TERM INT

# Web наружу: хост (Railway и т.п.) передаёт PORT, локально дефолт 8080.
cd /app/web
ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}" \
  exec dotnet CallCalendar.Web.dll
