# AGENTS.md

## Назначение проекта

Сервис бронирования календаря (аналог cal.com), который разрабатывается совместно с ИИ-агентом в рамках учебного проекта Хекслета «AI for Developers». Сейчас это каркас: цель — постепенно наращивать функциональность, не ломая автоматические проверки Hexlet.

## Что читать перед планированием

- `README.md` — назначение, стек и требования задачи. Этого достаточно для большинства задач.
- Если требований не хватает — уточнять у пользователя, а не догадываться.

## Ответственность частей проекта

- `src/CallCalendar.Web/` — Blazor Web App (Interactive Server). Здесь и UI, и серверная логика: точка входа `Program.cs`, интерфейс — Razor-компоненты в `Components/`, статика — в `wwwroot/`.
- `tests/CallCalendar.Web.Tests/` — xUnit + `Microsoft.AspNetCore.Mvc.Testing`; дымовые тесты старта приложения.
- `CallCalendar.sln` — решение; каждый проект добавляется в него.
- `Directory.Build.props` — общие свойства сборки (`net10.0`, `Nullable`, `ImplicitUsings`, `LangVersion`) для всех проектов.
- `global.json` — зафиксированная версия SDK.
- `.editorconfig` — правила стиля, используется линтером.
- `.github/workflows/ci.yml` — линтер, сборка и тесты на каждый push и PR.
- `.github/workflows/release-please.yml` + `release-please-config.json` + `.release-please-manifest.json` — выпуск версий по истории коммитов.

## Постоянные ограничения

- Blazor — это и бекенд, и фронтенд. Отдельного SPA-фронтенда нет и заводить его не нужно.
- Общие настройки сборки живут только в `Directory.Build.props`; не дублировать их в отдельных `.csproj`.
- Новые проекты — в `src/` (код) и `tests/` (тесты), каждый добавляется в `CallCalendar.sln`.
- Линтер — только `dotnet format`; сторонние анализаторы не подключаем.
- `<Version>` в `src/CallCalendar.Web/CallCalendar.Web.csproj` управляется release-please — не менять вручную.
- Нельзя удалять, переименовывать или править `.github/workflows/hexlet-check.yml` и переименовывать репозиторий: от них зависят автотесты Hexlet.

## Как проверять изменения

Перед завершением любой задачи эти команды должны проходить без ошибок:

```bash
dotnet format CallCalendar.sln --verify-no-changes
dotnet build CallCalendar.sln -c Release
dotnet test CallCalendar.sln -c Release
```

Ручной запуск приложения: `dotnet run --project src/CallCalendar.Web`.

## Что требует отдельного запроса

- `git push` и любые действия, меняющие удалённый репозиторий или настройки GitHub.
- Создание релизов, тегов и слияние pull request'ов.
- Удаление или переименование существующих файлов и каталогов.

## Соглашение о коммитах

- Только Conventional Commits: `feat:`, `fix:`, `chore:`, `test:`, `ci:`, `docs:`, `refactor:` и т.п.
- Формат: `<type>: <краткое описание>` — императив, нижний регистр, английский, без точки в конце.
- Правило распространяется и на коммиты, которые делает агент.
- `commitlint` в CI нет — это договорённость между пользователем и агентом.
- На версию и changelog влияют только `feat`/`fix`; остальные типы release-please скрывает.

## Agent skills

### Issue tracker

Задачи и спеки живут в GitHub Issues репозитория, все операции — через `gh` CLI. Подробности: `docs/agents/issue-tracker.md`.

### Triage labels

Пять канонических ролей триажа с одноимёнными лейблами (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). Подробности: `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `GLOSSARY.md` и `docs/adr/` в корне репозитория (создаются лениво через `/domain-modeling`). Подробности: `docs/agents/domain.md`.
