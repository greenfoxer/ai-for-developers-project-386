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

Запуск приложения:

```bash
dotnet run --project src/CallCalendar.Web
```

По умолчанию оно слушает `http://localhost:5175`. Чтобы поднять его на порту 5173 с доступом извне:

```bash
ASPNETCORE_URLS=http://0.0.0.0:5173 dotnet run --project src/CallCalendar.Web --no-launch-profile
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
