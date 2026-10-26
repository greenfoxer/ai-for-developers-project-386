# Календарь звонков


[![hexlet-check](https://github.com/greenfoxer/ai-for-developers-project-386/actions/workflows/hexlet-check.yml/badge.svg)](https://github.com/greenfoxer/ai-for-developers-project-386/actions)

Разработайте совместно с ИИ сервис для бронирования календаря

Учебный проект Хекслета: https://ru.hexlet.io/programs/ai-for-developers
Как это должно работать: https://files.hexlet.app/a/2ipc5m

## Стек

- .NET 10 / ASP.NET Core
- Blazor Web App (Interactive Server) — UI и сервер в одном приложении
- xUnit + `Microsoft.AspNetCore.Mvc.Testing` — тесты

## Установка

<!-- Опишите установку: клонирование, зависимости, переменные окружения -->

Нужен .NET SDK 10 (версия зафиксирована в `global.json`).

```bash
git clone https://github.com/greenfoxer/ai-for-developers-project-386.git
cd ai-for-developers-project-386
dotnet restore CallCalendar.sln
```

## Использование

Приложение состоит из двух процессов: API (`src/CallCalendar.Api`) и веб-интерфейс (`src/CallCalendar.Web`). Сначала поднимите API, затем Web.

### Запуск API

```bash
dotnet run --project src/CallCalendar.Api
```

API слушает `http://localhost:5000` (порт совпадает с `@server` TypeSpec-контракта). Данные хранятся в SQLite (`src/CallCalendar.Api/callcalendar.db`); при первом старте создаются два демо-типа событий.

### Запуск Web

```bash
dotnet run --project src/CallCalendar.Web
```

По умолчанию Web слушает `http://localhost:5175` и обращается к API по адресу `http://localhost:5000` (переопределяется через `CallCalendarApi:BaseUrl`). Чтобы поднять Web на порту 5173 с доступом извне:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://0.0.0.0:5173 dotnet run --project src/CallCalendar.Web --no-launch-profile
```

`ASPNETCORE_ENVIRONMENT=Development` здесь обязателен: `--no-launch-profile` отбрасывает переменные из launchSettings, а без Development-окружения приложение не подгружает манифест статических ассетов (`dotnet run` работает не из published-выхода) — страницы открываются, но CSS/JS отдают 500.

### Генерация контракта

Контракт описан в TypeSpec (`contracts/`), из него генерируются OpenAPI-спецификация, клиентский SDK (Kiota) и серверные контроллеры (NSwag). Одна команда пересобирает всю цепочку и прогоняет проверки:

```bash
./scripts/generate.sh
```

Проверки (линтер, сборка, тесты):

```bash
dotnet format CallCalendar.sln --verify-no-changes
dotnet build CallCalendar.sln -c Release
dotnet test CallCalendar.sln -c Release
```

---

<details>
<summary>Автоматические тесты Хекслета</summary>

Тесты запускаются на каждый коммит. За запуск отвечает файл `.github/workflows/hexlet-check.yml` — не удаляйте и не переименовывайте ни его, ни репозиторий.

</details>

## О Хекслете

[Хекслет](https://ru.hexlet.io/) — школа программирования: авторские программы обучения с практикой, поддержкой наставников и реальными проектами, которые остаются в резюме. Этот репозиторий — один из таких проектов.

## Docker (локальный запуск)

Единый образ собирает `CallCalendar.Api` и `CallCalendar.Web` в одном контейнере. БД SQLite эфемерная — при каждом старте контейнера она пересоздаётся с сид-данными (без томов).

Сборка и запуск через `docker compose`:

```bash
docker compose up --build
```

По умолчанию доступно на `http://localhost:8080` (можно переопределить `APP_PORT`). Для проверки: `/`, `/book` (визард), `/admin` (панель владельца). Перезапуск контейнера/`docker compose restart` даёт чистую БД.

Сборка и запуск напрямую:

```bash
docker build -t callcalendar:latest .
PORT=8080 docker run --rm -p 8080:8080 -e PORT=8080 callcalendar:latest
```

## MCP-серверы

В ходе разработки MCP-серверы в боевом сценарии не использовались, поэтому конфигурации MCP в репозитории нет. Что пробовали и почему отказались:

- **Render (хостинг)** — MCP-сервер подключён, но его инструмент `create_web_service` не поддерживает Docker, а Blueprint требует платного плана. Деплой в итоге выполнен на Railway тем же Docker-образом (см. «Деплой»).
- **GitHub и прочие MCP-серверы** — не использовались из-за ограничений хостовой среды; операции с issues ведутся через `gh` CLI.

## Деплой

**Railway** (https://railway.app): используется `railway.json` (Dockerfile-сборка). После мержа в `main` — New Project → Deploy from GitHub repo, Railway сам соберёт и задеплоит с автогенерацией `PORT`.

Про Render: MCP-сервер хостинга был подключён, но его инструмент создания сервисов не поддерживает Docker (`create_web_service` работает только с не-Docker сервисами), а Blueprint у Render требует платного плана. Поэтому деплой выполнен на Railway тем же Docker-образом: Railway собирает его по `railway.json`/`Dockerfile`, приложение слушает выданный платформой `PORT` (`entrypoint.sh`: `ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}`).

Публичный экземпляр: **https://ai-for-developers-project-386-production-af88.up.railway.app**

