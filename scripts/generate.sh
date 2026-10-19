#!/usr/bin/env bash
# Одна команда всей цепочки генерации (wayfinder E5, #20):
#   TypeSpec -> OpenAPI -> Kiota-клиент -> NSwag-сервер -> dotnet format/build/test.
# Идемпотентна: повторный запуск даёт тот же закоммиченный выход.
# Требования: Node >=22 (только для tsp compile), .NET SDK 10 (global.json), npm.
set -euo pipefail
cd "$(dirname "$0")/.."

# 0) Локальные dotnet-инструменты (версии закреплены в .config/dotnet-tools.json).
dotnet tool restore

# 1) TypeSpec -> OpenAPI 3.0.
#    --no-bin-links: в drvfs-деревьях (/mnt/d) создание .bin-симлинков запрещено EPERM (#16);
#    бинарь tsp вызывается напрямую через node.
( cd contracts
  npm ci --no-audit --no-fund --no-bin-links
  node node_modules/@typespec/compiler/cmd/tsp.js compile . --warn-as-error )

# 2) OpenAPI -> клиентский SDK (src/CallCalendar.Client/Generated).
dotnet kiota generate -l CSharp -c CallCalendarClient -n CallCalendar.Client \
  -d contracts/tsp-output/openapi/openapi.yaml \
  -o src/CallCalendar.Client/Generated \
  --exclude-backward-compatible --clean-output

# 3) OpenAPI -> серверные артефакты (src/CallCalendar.Api/Generated).
#    Правила из #16: UseActionResultType (коды 201/409), без /DateType (DateFormatConverter).
dotnet nswag openapi2cscontroller \
  /input:contracts/tsp-output/openapi/openapi.yaml \
  /output:src/CallCalendar.Api/Generated/Controllers.cs \
  /namespace:CallCalendar.Api \
  /ControllerStyle:Partial /ControllerTarget:AspNetCore \
  /GenerateModelValidationAttributes:true /UseActionResultType:true

# 4) Основной контур проверок репозитория.
dotnet format CallCalendar.sln --verify-no-changes
dotnet build CallCalendar.sln -c Release
dotnet test CallCalendar.sln -c Release

echo
echo "Цепочка пройдена: контракты и артефакты актуальны, формат/сборка/тесты зелёные."
