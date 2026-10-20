#!/bin/sh
# Совмещённый рантайм: API (localhost:5000) + Blazor Web (0.0.0.0:$PORT) в одном контейнере.
set -eu

# Эфемерная БД: на каждый старт — чистая база (EnsureCreated + сид поднимут её заново).
mkdir -p /app/data
rm -f /app/data/callcalendar.db /app/data/callcalendar.db-shm /app/data/callcalendar.db-wal 2>/dev/null || true

# API слушает localhost:5000 внутри контейнера — как @server в контракте.
ConnectionStrings__sqlite="Data Source=/app/data/callcalendar.db" \
  dotnet /app/api/CallCalendar.Api.dll &
api_pid=$!
trap 'kill "$api_pid" 2>/dev/null || true' TERM INT

# Web наружу: Render передаёт PORT, локально дефолт 8080.
ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}" \
  exec dotnet /app/web/CallCalendar.Web.dll
