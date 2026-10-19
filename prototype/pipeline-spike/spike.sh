#!/usr/bin/env bash
# THROWAWAY (wayfinder #16): полная регенерация цепочки + сборка + wire-прогон.
set -euo pipefail
cd "$(dirname "$0")"
export PATH="$PATH:$HOME/.dotnet/tools"

# 1) TypeSpec -> OpenAPI 3.0
( cd contracts
  npm install --no-audit --no-fund --no-bin-links
  node node_modules/@typespec/compiler/cmd/tsp.js compile . --warn-as-error )

# 2) OpenAPI -> Kiota клиент (net10 dotnet tool, Node не нужен)
kiota generate -l CSharp -c CallCalendarClient -n CallCalendar.Spike.Client \
  -d contracts/tsp-output/openapi/openapi.yaml -o src/SpikeClient/Generated \
  --exclude-backward-compatible --clean-output

# 3) OpenAPI -> NSwag серверные артефакты (частальные контроллеры + DTO)
nswag openapi2cscontroller /input:contracts/tsp-output/openapi/openapi.yaml \
  /output:src/SpikeApi/Generated/Controllers.cs /namespace:CallCalendar.Spike.Api \
  /ControllerStyle:Partial /ControllerTarget:AspNetCore \
  /GenerateModelValidationAttributes:true /UseActionResultType:true

# 4) Сборка всех spike-проектов
dotnet build Spike.slnx -c Release

# 5) Живой wire-прогон: Kiota-клиент -> NSwag-API (enum/date/time/201/409)
dotnet run --project src/SpikeApi -c Release --no-build >spike-api.log 2>&1 &
api_pid=$!
trap 'kill "$api_pid" 2>/dev/null || true' EXIT
sleep 3
dotnet run --project src/SpikeRunner -c Release --no-build
