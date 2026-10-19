# Research: TypeSpec tooling + .NET 10 (ticket #13)

## Question (restatement)

Факты по TypeSpec-тулингу в связке с .NET 10 в репозитории вида «Blazor Web App, API-first pipeline: TypeSpec → OpenAPI → Kiota-клиент + сгенерированные серверные артефакты»:

- актуальные пакеты/версии `@typespec/compiler`, `@typespec/http`, `@typespec/openapi3` (npm);
- какой формат/версию OpenAPI эмитит openapi3-эмиттер (3.0 vs 3.1), правила выравнивания версий compiler/emitter;
- как запускать `tsp compile` в репо/CI: требования к node/npm, типичная структура папок (`main.tsp`, `tspconfig.yaml`), конфигурация эмиттера, выходная директория;
- как Node-генерация обычно интегрируется с dotnet-сборкой (pre-build step, отдельный скрипт, Makefile) — типовые паттерны;
- подводные камни: матрица совместимости эмиттеров, breaking changes 2025–2026, различия Windows/Linux.

Только факты со ссылками на первоисточники; решений не принимаем.

## Sources & method note

Первоисточники, использованные в этом документе:

- **npm registry** (`registry.npmjs.org`) — метаданные пакетов: версии, `engines`, `peerDependencies`, даты релизов. Это authoritative источник для версий/совместимости.
- **Репозиторий microsoft/typespec (GitHub, ветка main)** — README эмиттеров, changelogs и markdown-исходники сайта документации (`website/src/content/docs/...`). Документация на **typespec.io** генерируется из этих же файлов, поэтому там же можно читать по URL вида `https://typespec.io/docs/...`.
- **Живой прогон** — установка `@typespec/compiler@1.17.0` + `@typespec/openapi3@1.17.0` на Node 22.22.1 (Linux) и компиляция минимального спека; вывод помечен как «empirical» и датируется 2026-10-19.
- Прямой fetch `typespec.io` и `learn.microsoft.com` из агента возвращал HTTP 403 (CDN-блокировка гео/бота), поэтому для этих страниц используются эквивалентные raw-файлы из тех же репозиториев GitHub.

---

## 1. Актуальные пакеты и версии (состояние на 2026-10-19)

| Пакет | latest | Дата релиза | Node engine |
|---|---|---|---|
| `@typespec/compiler` | **1.17.0** | 2026-10-08 | `>=22.0.0` |
| `@typespec/http` | **1.17.0** | 2026-10-08 | `>=22.0.0` |
| `@typespec/openapi3` | **1.17.0** | 2026-10-08 | `>=22.0.0` |
| `@typespec/openapi` | 1.17.0 | 2026-10-08 | — |
| `@typespec/json-schema` | 1.17.0 | 2026-10-08 | `>=22.0.0` |
| `@typespec/rest` | **0.87.0** | 2026-10-08 | — |
| `@typespec/versioning` | **0.87.0** | 2026-10-08 | — |
| `@typespec/http-server-csharp` | **0.58.0-alpha.33** | 2026-10-08 | `>=22.0.0` |

- Версии взяты из npm registry: `https://registry.npmjs.org/@typespec/compiler/latest`, `.../latest` каждого пакета; даты — `npm view <pkg> time` (данные registry).
- Ядро (`compiler`, `http`, `openapi3`, `openapi`, `json-schema`) **релизнутым lockstep-поездом одной версии 1.17.0**; `rest`/`versioning` живут на отдельной 0.x-линейке (0.87.0 того же релизного поезда). Источник: `peerDependencies` в метаданных пакетов (см. §3).
- Все пакеты опубликованы под лицензией **MIT** (`license: "MIT"` в метаданных npm, там же) — для корпоративного CI лицензионных блокеров нет; сама тулchain — Microsoft (`microsoft/typespec`).
- Compiler и эмиттеры ставятся как **npm-пакеты**; `@typespec/compiler` содержит CLI `tsp` (`bin: { tsp: cmd/tsp.js }`) — источник: метаданные пакета.
- Экспериментальный эмиттер серверных C#-артефактов `@typespec/http-server-csharp` **находится в alpha** (`latest` = 0.58.0-alpha.33, все релизы — пре-релизы). Источник: `https://registry.npmjs.org/@typespec/http-server-csharp`. В official-туториале он используется вместе с dotnet-сборкой («Prerequisites: .NET 9 installed (required for C# server code generation)») — `.NET 9`, не 10: <https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/getting-started/code-generation-csharp.md> (raw: <https://raw.githubusercontent.com/microsoft/typespec/main/website/src/content/docs/docs/getting-started/code-generation-csharp.md>).

## 2. Что эмитит `@typespec/openapi3`: 3.0 по умолчанию, 3.1/3.2 опционально

- Эмиттер умеет **OpenAPI 3.0, 3.1 и 3.2**: опция `openapi-versions` типа `"3.0.0" | "3.1.0" | "3.2.0"`, **default: `["3.0.0"]`**. Источник: README эмиттера, «Emitter options → openapi-versions»: <https://github.com/microsoft/typespec/blob/main/packages/openapi3/README.md> (raw: <https://raw.githubusercontent.com/microsoft/typespec/main/packages/openapi3/README.md>).
- Историческая справка (changelog): 3.1 появилось за опцией `openapi-versions` в 0.64.0 («Open API 3.0 is emitted by default»), 3.2.0 — в 1.6.0, опция «экспозится» в 1.9.0 («both 3.1.0 and 3.2.0 are implemented»): <https://github.com/microsoft/typespec/blob/main/packages/openapi3/CHANGELOG.md>.
- **Empirical (2026-10-19, Linux, Node 22.22.1):** минимальный `main.tsp` + `tspconfig.yaml` с `emit: ["@typespec/openapi3"]` и без опций → `tsp compile .` создал `tsp-output/@typespec/openapi3/openapi.yaml` с первой строкой `openapi: 3.0.0`. С `openapi-versions: [3.1.0]` и `emitter-output-dir: "{output-dir}/openapi"` → тот же spec эмитит `openapi: 3.1.0` в `tsp-output/openapi/openapi.yaml`.
- Формат вывода: `file-type: "yaml" | "json" | массив` — **default `yaml`** (можно эмитить оба формата за один прогон, массив поддерживается с 1.10.0, PR #9890). `output-file` по умолчанию `{service-name-if-multiple}.{version}.openapi.yaml` с интерполяцией имени сервиса/версии. Источник: README (там же).
- `new-line` по умолчанию `"lf"` — вывод одинаковый на Windows и Linux (README, «new-line»).
- **Важно для пайплайна TypeSpec → OpenAPI → Kiota:** Kiota **умеет OpenAPI 3.1 с версии 1.24.0 (2025-03-12)**: changelog «Added support for OpenAPI 3.1. [#3914]» — <https://github.com/microsoft/kiota/blob/main/CHANGELOG.md>; PR microsoft/kiota#5936 (closed 2025-02-24) — <https://github.com/microsoft/kiota/pull/5936>. Актуальная версия Kiota **v1.36.1 (2026-10-12)**, CLI-тул под .NET с `TargetFrameworks net8.0;net9.0;net10.0` — .NET 10 поддерживается: <https://api.github.com/repos/microsoft/kiota/releases/latest> и <https://github.com/microsoft/kiota/blob/main/src/kiota/kiota.csproj>.
- Замечание по семантике 3.1: на 3.1+ эмиттер кладёт `description` и другие sibling-keywords рядом с `$ref` (PR #11185, 1.14.0) и использует `unevaluatedProperties` для sealed-схем; на 3.0 поведение другое — выход зависит от выбранной версии (changelog openapi3). Опция `enum-strategy: annotated` работает только на 3.1+ (README).

## 3. Правила выравнивания версий compiler ↔ библиотеки/эмиттеры

- **Peer-dependencies задают матрицу совместимости.** `@typespec/openapi3@1.17.0` объявляет `peerDependencies`: `@typespec/compiler ^1.17.0` (required), `@typespec/http ^1.17.0` (required), `@typespec/openapi ^1.17.0` (required); опциональные (peerDependenciesMeta.optional): `@typespec/versioning ^0.87.0`, `@typespec/json-schema ^1.17.0`, `@typespec/sse ^0.87.0`, `@typespec/events ^0.87.0`, `@typespec/streams ^0.87.0`. `@typespec/http@1.17.0` требует `@typespec/compiler ^1.17.0`. Источник: метаданные npm registry (URL во §1). То есть **все ядро-пакеты должны быть одного минорного поезда** (1.17.x).
- Компилер имеет отдельную диагностику `incompatible-compiler-version`, которую он репортит, когда загружает библиотеку, чей peer-диапазон не покрывает версию компилера (упоминается в changelog compiler 1.3.0, PR #7957 «Fix incorrectly reported `incompatible-compiler-version` … when using symlinks»): <https://github.com/microsoft/typespec/blob/main/packages/compiler/CHANGELOG.md>.
- **Policy «одно экземпляра на процесс»:** «TypeSpec runs in a plugin system and it expects that only a single instance of the compiler and each library is loaded at once… If different versions are used a warning will be emitted and there is no guarantee that it will be working» (пример: проект на compiler 1.2.0 + эмиттер на 1.3.0 не гарантированно работает). Источник: Breaking Change Policy → «Loading multiple instances of the compiler/libraries»: <https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/breaking-change-policy.mdx> (raw: <https://raw.githubusercontent.com/microsoft/typespec/main/website/src/content/docs/docs/handbook/breaking-change-policy.mdx>). Практический вывод для monorepo: **не допускать двух копий `@typespec/compiler` в дереве node_modules** (один package.json/lock на все tsp-проекты или workspaces).
- **Node policy:** «TypeSpec will support all current, active, and maintenance LTS Node.js releases» + шестимесячный grace после EOL; поддержка альтернативных JS-рантаймов не гарантируется (там же, «Supported Node.js Versions»). Сейчас `engines.node >= 22.0.0` у compiler/http/openapi3; порог 22 поднялся в **compiler 1.12.0 (2026-05-12)** — до этого с 1.0.0 (2025-05-06) было `>=20.0.0`, до 0.67.0 — `>=18` (проверено по `npm view @typespec/compiler <ver> engines` для 1.0.0…1.17.0). Для CI это означает: Node 22 (maintenance LTS) или 24 (active LTS) через `actions/setup-node`.
- **Breaking change policy для ≥1.0.0:** «A spec that builds successfully with a version of the TypeSpec compiler and its libraries will build successfully with a newer version … within the same major version» — внутри 1.x спеки не ломаются; новое функциональности в minor-релизах может не поддерживаться старыми эмиттерами (там же, «Breaking Change Philosophy»). Выход эмиттеров stability-гарантий не даёт: «Emitter output are not guaranteed to be producing the exact same output as the previous version for the same input» (там же, «Emitter output»).

## 4. Как запускать `tsp compile`: layout, CLI, конфиг, вывод

### Установка и prerequisites

- Нужен **Node.js и npm**: официальное руководство по установке — «The minimum versions required are Node.js 22.0.0 and npm 7.0.0»; `npm install -g @typespec/compiler` для CLI, `tsp --version` для проверки. Источник: Getting Started with TypeSpec for REST: <https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/getting-started/getting-started-rest/01-setup-basic-syntax.mdx>.
- Глобальная установка — вариант для разработки; для CI воспроизводимость достигается пиннингом версии пакета в `package.json` репозитория + lock-файлом (см. ниже «Reproducibility»).

### Типичная структура проекта

Из того же getting-started-документа («Project Structure Overview») — результат `tsp init` (шаблон «Generic REST API») + `tsp install` + `tsp compile .`:

```
- main.tsp            # точка входа TypeSpec-определений
- tspconfig.yaml      # конфиг компилера/эмиттеров
- package.json        # зависимости (@typespec/*)
- node_modules/
- tsp-output/
  - @typespec/
    - openapi3/
      - openapi.yaml  # результат генерации
```

- `tsp init` интерактивно создаёт проект; `tsp install` ставит зависимости; `tsp compile .` компилирует директорию с `main.tsp`; `tsp compile . --watch` — пересборка на сохранение (там же).
- Альтернативные имена входа разрешимы: приоритет резолва entrypoint — `entrypoint` из `tspconfig.yaml` (`kind: project`) → `package.json`/`tspMain` → `main.tsp` (configuration docs: <https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/configuration/configuration.mdx>; фикс приоритета — compiler changelog 1.14.0, PR #11235).
- C#-туториал показывает layout «monorepo-friendly»: папка TypeSpec содержит `main.tsp`, `tspconfig.yaml`, `package.json`, а вывод — `tsp-output/schema/openapi.yaml` + `tsp-output/server/...` (code-generation-csharp.md, там же §1).

### Конфигурация эмиттера (`tspconfig.yaml`)

Канонический пример из туториала (code-generation-csharp.md) — два эмиттера за один прогон:

```yaml
emit:
  - "@typespec/openapi3"
  - "@typespec/http-server-csharp"
options:
  "@typespec/openapi3":
    emitter-output-dir: "{output-dir}/schema"
    openapi-versions:
      - 3.1.0
  "@typespec/http-server-csharp":
    emitter-output-dir: "{output-dir}/server/generated"
```

- `output-dir` в конфиге задаёт корень вывода (default — `tsp-output` рядом с проектом), `emitter-output-dir` по умолчанию `{output-dir}/@typespec/openapi3` (README openapi3, «emitter-output-dir»). Интерполяция поддерживает `{cwd}`, `{project-root}`, `{output-dir}`, `{service-name}`, `{version}`, env-vars и кастомные `parameters` (configuration.mdx, «Interpolation of Emitter Path Config»).
- Ключевые CLI-флаги `tsp compile` (empirical help, v1.17.0): `--output-dir <path>`, `--emit <name>`, `--options/--option <emitter.key=value>`, `--config <file|dir>`, `--watch`, `--warn-as-error`, `--no-emit`, `--dry-run`, `--list-files`, `--stats`, `--arg name=value`.
- **Коды возврата (empirical, 2026-10-19):** ошибка диагностики → exit 1; успешная компиляция → exit 0; `--warn-as-error` при отсутствии предупреждений → exit 0. То есть `tsp compile . --warn-as-error` безопасен как CI-гейт.
- Для закрытых/корпоративных mirror'ов npm есть env-переменная `TYPESPEC_NPM_REGISTRY` — `tsp init`/`tsp install` ходят в неё (compiler changelog 1.10.0 PR #9803; handbook CLI docs: <https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/cli.md>). Это же решает «офлайн»-часть вопроса: загруженные tarball'ы кэшируются npm, а воспроизводится это lock-файлом (см. ниже).

### Воспроизводимость / lock-файлы

- Официальный документ «Reproducibility»: коммитить **`package-lock.json`**; в CI использовать **`npm ci`** («ensures that package.json and package-lock.json are in sync»); для эволюции API — `@typespec/versioning` и пиннинг спеки по commit SHA: <https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/reproducibility.md>.

## 5. Интеграция Node-генерации с dotnet-сборкой: распространённые паттерны

Первичных источников «единственно правильного» способа нет — в самом TypeSpec-репо описаны несколько рабочих паттернов:

1. **TypeSpec-папка как отдельный Node-проект, генерация — отдельным шагом до `dotnet build`** (паттерн туториала code-generation-csharp.md): `tsp compile .` (эмитит OpenAPI + C#-заготовки), затем `dotnet run/build --project <generated>`. Scaffold-инструмент вызывается через `npx` локально из папки проекта: `npx hscs-scaffold . --use-swaggerui --overwrite`, и отдельно отмечается: «The npx command executes binaries from your local node_modules directory… especially useful if you have multiple TypeSpec projects with different versions». Сгенерированные файлы помечены «regenerated whenever you recompile», ручные implementation-файлы не перезаписываются — то есть разделение generated/impl-кода.
2. **Пиннинг tool-версий в отдельной eng-папке + `npm exec --prefix` из корня репо** (паттерн Azure SDK for .NET): `eng/common/tsp-client/package.json` c pinned `@azure-tools/typespec-client-generator-cli` + коммитнутый `package-lock.json`, в CI `npm ci --prefix eng/common/tsp-client`, запуск `npm exec --prefix … -- tsp-client generate --output-dir ./generated`; обновления через Dependabot. Источник: <https://github.com/Azure/azure-sdk-for-net/blob/main/eng/common/tsp-client/README.md> (raw: <https://raw.githubusercontent.com/Azure/azure-sdk-for-net/main/eng/common/tsp-client/README.md>); там же `engines.node >= 22.13.0`.
3. **CI как два независимых шага в GitHub Actions**: `actions/setup-node` (v22/v24) + `npm ci` + `npx tsp compile .` → затем `actions/setup-dotnet` + `dotnet build`. Основание: CI у TypeSpec-проектов в монорепе microsoft/typespec сам построен на Node-тулинге, у kiota — на dotnet-тулинге; объединение — ординаарный workflow-step паттерн (в ci.yml этого репо уже есть `setup-dotnet` + `dotnet build`). Отдельного «официального» GitHub Action для TypeSpec не найдено: поиск по `typespec-action` в GitHub не дал публичных репозиториев (empirical через GitHub Search API, 2026-10-19).
4. **Kiota-шаг может не зависеть от Node вообще**: `kiota` ставится и как dotnet-тул (`Microsoft.OpenApi.Kiota`), и через npm/brew/docker — README kiota ссылается на «available options» (<https://learn.microsoft.com/openapi/kiota/install>; page fetch 403 — цитируется по README <https://github.com/microsoft/kiota/blob/main/README.md>). То есть Node в дотнетовой CI-коробке нужен только для `tsp compile`, а генерация клиента из уже скомпилированного `openapi.yaml` — чистый dotnet/`kiota generate`.
5. **MSBuild-hook как вариант, не документированный Microsoft**: в туториале Microsoft generation-шаг явно вынесен ДО `dotnet run` как отдельная команда, а не в `<Target BeforeTargets="Build">`. Документированных guidance по встраиванию `tsp compile` в MSBuild-таргеты в первичных источниках не найдено — это community-паттерн, в этом документе остаётся как «известный, но не Microsoft-документированный» вариант.

## 6. Подводные камни (2025–2026)

- **Breaking changes на переходе 0.x → 1.0 (2025-05-06 — compiler, http, openapi3 одновременно вышли на 1.0.0):** с 1.0 действует semver-policy «спеки не ломаются внутри major» (§3). Ключевые ломки 1.0.0-rc: `bodyKind: "file"` для файловых тел и удаление implicit multipart (openapi3 changelog, PR #6557/#6563); в compiler 1.0.0 удалён `version` из `@service` (PR #7222) и typekit переехал из `/experimental` (PR #7193). Большинство туториалов 2024–early2025 (и много learn.microsoft.com-материалов) написаны под 0.x API/sintaksis — сверять по changelog.
- **Value-синтаксис `#{}`/`[]`** вместо `{}`/`[]` для value-аргументов декораторов — в 1.x `@service({ title: "x" })` **не компилируется** («expect-value: … Use #{} to create an object value») — empirical 2026-10-19. Старые сниппеты из 0.x-документации не работают как есть.
- **compiler 1.15.0 (2026-08-11) — breaking:** `using` до file-level (blockless) namespace теперь резолвится от глобального namespace, а не от файлового (PR #11552, compiler changelog).
- **0.67.0 → 1.12.0: Node floor двигался 18 → 20 → 22.** На сегодня `engines >= 22.0.0` (empirical `npm view` по версиям; getting-started docs §4). Runner'ы ubuntu-latest/setup-node с Node 20 больше не годятся.
- **openapi3 1.16.0 (2026-09-09) — security fix:** интерполяция `{version}`/`{service-name}` в `output-file` санитизируется, чтобы нельзя было записать файл за пределы output-dir (PR #11777). Практическое следствие: на старых версиях имена версий/неймспейсов с `/` или `..` влияли на путь вывода.
- **Эмиттер-вывод не byte-stable** между версиями (§3, policy «Emitter output») — не делать проверку «detected diff of openapi.yaml» единственной защитой; коммитить lock + при необходимости нормализованное сравнение (npm-овский changelog 1.14.0 PR #11185 менял форму `$ref`-sibling на 3.1+ без изменения семантики).
- **`incompatible-compiler-version`/duplicate compiler instances** — если эмиттер и tsp живут в разных node_modules с разными версиями, компилер грузит две копии; официально «no guarantee» (§3). Фиксы в changelog: #6883 (multiple compiler instances realm-проблема), #9320 (`--list-files` при нескольких копиях), #7957 (symlink-ложные предупреждения).
- **C#-серверный эмиттер `@typespec/http-server-csharp` — alpha** (0.58.0-alpha.33, все пре-релизы; §1) и в туториале требует **.NET 9** (не 10) как declared prerequisite. Для «generated server artifacts» на .NET 10 это факт-ограничение, не догма — но на момент исследования stable-версии нет.
- **Kiota ↔ 3.1**: поддержка 3.1 только с 1.24.0 (2025-03-12); до этого — только 3.0 (changelog kiota §2). Актуальная 1.36.1 имеет net10.0 TFM. Часть 3.1-фич дошлифовывается и позже (webhooks-модели #6394, `$dynamicRef` #7815 — phases 2025–2026).
- **Windows/Linux:** функционально тулchain кроссплатформенный (один npm-пакет, вывод LF по умолчанию — §2). Известные Windows-специфичные дефекты (исправлены, но показывают класс проблем): UNC-пути сетевых шар — `tsp compile` падал с `INVALID_MODULE_EXPORT_TARGET` (фикс 1.17.0, compiler changelog PR #11998); некорректные относительные пути в диагностике (фикс 1.1.0, PR #7524); `tsp compile` из чужой директории давал невалидный документ (фикс 0.58.0, PR #3516). Для CI ubuntu-latest — рисков нет; для локальной разработки на Windows — держать рабочий проект вне сетевых UNC-шар и на актуальном compiler.
- **Лицензии/offline:** MIT (§1); офлайн/air-gapped CI закрывается `TYPESPEC_NPM_REGISTRY` для mirror'а (§4) и коммитнутым `package-lock.json` + `npm ci` (§4, Reproducibility).

## 7. Implications for this repo (факты, оформленные как варианты)

Контекст репо: `src/CallCalendar.Web` (Blazor Interactive Server), .NET 10 (`Directory.Build.props`), CI: ubuntu-latest, `setup-dotnet@v4` + `dotnet build/test` (`.github/workflows/ci.yml`), без Node-шагов сейчас.

- **Версии для пайплайна TypeSpec → OpenAPI → Kiota:** `@typespec/compiler/http/openapi3` брать lockstep **1.17.0** (или следующий minor того же поезда) + Node **>=22** в CI; `rest`/`versioning` — отдельная 0.87.x-линейка тех же поездов (§1, §3).
- **Формат OpenAPI — решение контракта, а не лимитация тула:** эмиттер по умолчанию даёт **3.0.0**, 3.1.0 — явная опция `openapi-versions` (empirical §2); **обе версии Kiota потребляет** (3.1 с 1.24.0, §2/§6). Если нужен 3.1 — его надо включить опцией и пинить, иначе дефолт « silently 3.0».
- **Layout без правок dotnet-части:** отдельная папка (например `specs/` или `tsp/`) с `main.tsp` + `tspconfig.yaml` + `package.json` + `package-lock.json`; вывод — `emitter-output-dir: "{output-dir}/openapi"` → `specs/tsp-output/openapi/openapi.yaml`, который передаётся Kiota как `--openapi`-вход (§4, §5.1). AGENTS-констрейнт «общие настройки только в Directory.Build.props» не конфликтует: node-настройки живут в package.json tsp-папки (§5.2, паттерн Azure SDK eng-папки).
- **CI-интеграция — варианты:** (a) отдельные шаги `setup-node` + `npm ci` + `npx tsp compile . --warn-as-error` перед dotnet-шагами (exit-коды гейтят сборку, empirical §4); (b) генерация как отдельный ручной/скриптовый шаг с коммитом результата `openapi.yaml`, а dotnet-CI только проверяет, что закоммиченный артефакт соответствует спеке (пересобрал → diff). Оба варианта встречаются в первичных источниках (§5.1 туториал «separate step», §5.2 пиннинг в eng-папке); MSBuild-таргет для `tsp` Microsoft не документирует (§5.5).
- **Kiota-генерация клиента не требует Node:** `kiota` как dotnet-тул с net10.0 (§5.4) — Node в контейнере нужен только для `tsp compile`.
- **«Server artifacts»-генерация сегодня:** единственный Microsoft-эмиттер серверного C#-кода (`@typespec/http-server-csharp`) в alpha и на .NET 9 prerequisite (§1/§6); для .NET 10 это вариант «accept preview» vs «свой scaffold + contract-first только на клиенте». Факт для планирования, не рекомендация.
- **Дисциплина версий:** один node_modules на tsp-папку (не две копии compiler), lock-файл в git, Dependabot-обновления поезда версий; внутри 1.x спеки не ломаются, но эмиттер-вывод может меняться → диффы `openapi.yaml` в PR воспринимать как expected (§3/§6).
- **Лицензионных и offline-блокеров нет:** MIT + `TYPESPEC_NPM_REGISTRY`/`npm ci` для mirror'а (§1/§4).

## Raw source list (для проверки)

- npm registry metadata: `https://registry.npmjs.org/@typespec/compiler/latest`, `https://registry.npmjs.org/@typespec/http/latest`, `https://registry.npmjs.org/@typespec/openapi3/latest`, `https://registry.npmjs.org/@typespec/http-server-csharp`
- openapi3 README (эмиттер-опции): `https://github.com/microsoft/typespec/blob/main/packages/openapi3/README.md`
- openapi3 CHANGELOG: `https://github.com/microsoft/typespec/blob/main/packages/openapi3/CHANGELOG.md`
- compiler CHANGELOG: `https://github.com/microsoft/typespec/blob/main/packages/compiler/CHANGELOG.md`
- Getting Started (REST): `https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/getting-started/getting-started-rest/01-setup-basic-syntax.mdx` (зеркало: typespec.io/docs/getting-started/getting-started-rest/01-setup-basic-syntax)
- C# code generation tutorial: `https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/getting-started/code-generation-csharp.md`
- Breaking change policy: `https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/breaking-change-policy.mdx`
- Reproducibility: `https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/reproducibility.md`
- Configuration: `https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/configuration/configuration.mdx`
- CLI: `https://github.com/microsoft/typespec/blob/main/website/src/content/docs/docs/handbook/cli.md`
- Azure SDK for .NET tsp-client eng-папка: `https://github.com/Azure/azure-sdk-for-net/blob/main/eng/common/tsp-client/README.md`, `https://github.com/Azure/azure-sdk-for-net/blob/main/eng/common/tsp-client/package.json`
- Kiota CHANGELOG/README/Release: `https://github.com/microsoft/kiota/blob/main/CHANGELOG.md`, `https://github.com/microsoft/kiota/blob/main/README.md`, `https://github.com/microsoft/kiota/releases` (v1.36.1, 2026-10-12), `https://github.com/microsoft/kiota/pull/5936`
- Empirical run: `npx tsp compile` v1.17.0 на Node 22.22.1 / Linux, 2026-10-19 (exit-коды, layout вывода, openapi 3.0.0/3.1.0).
