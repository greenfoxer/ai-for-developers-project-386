# Pipeline spike (THROWAWAY, wayfinder #16)

Одноразовый прототип сквозной цепочки **TypeSpec → OpenAPI → Kiota-клиент → NSwag-сервер →
Blazor-вызов → живой wire-обмен**. Это НЕ заготовка продакшена: код помечен THROWAWAY, ветка
`prototype/pipeline-spike` в main не мержится. Вопрос тикета: «проходима ли вся цепочка выбранным
стеком (R1/R2/R3) и сколько места занимает генерация».

## Вердикт: да, проходима, с 6 правилами пайплайна

Чистый прогон цепочки: `tsp compile` → `kiota generate` → `nswag openapi2cscontroller` →
`dotnet build Spike.slnx` (0 warnings) → старт API → Kiota-клиент из Blazor-компонента делает 4
реальных вызова, включая 201 с Created-заголовком и 409, прилетающий типизированным
`ProblemDetails`-исключением.

Правила, без которых цепочка ломается (все эмпирически проверены здесь):

1. **`@tag` на интерфейсах обязателен.** Без него OpenAPI-теги пусты, и NSwag сваливает все
   операции в один `Controller`/`IController`. В TypeSpec 1.17 `@tag` — стандартный декоратор
   компилятора; теги НЕ наследуются автоматически от имени интерфейса.
2. **`@server` в контракте обязателен для Kiota.** Иначе у клиента нет base URL (генератор
   предупреждает), и Blazor-стороне придётся задавать его вручную.
3. **NSwag серверные артефакты нужны с `/UseActionResultType:true`.** Дефолтный `Task<T>`
   теряет коды ответов: 201 Created и 409 Conflict превращаются в 200. С `ActionResult<T>`
   реализация сама возвращает `CreatedResult`/`ConflictObjectResult` — статусы сохраняются.
4. **API обязан использовать Newtonsoft-форматтеры** (`AddControllers().AddNewtonsoftJson()`),
   т.к. NSwag-DTO генерируются с Newtonsoft-атрибутами: `StringEnumConverter` для enum и
   `DateFormatConverter` для `format: date`. Дефолтный STJ в ASP.NET Core 10 эти атрибуты
   игнорирует: enum уедет числом, date — с временем в строке.
5. **`/DateType` у NSwag лучше НЕ включать.** С `DateType:DateTime` пропадает
   `DateFormatConverter`, и `date` уезжает на провод как `"2026-03-05T00:00:00"` вместо
   `format: date`. Дефолт `DateTimeOffset` + конвертер — корректный `"2026-03-05"`.
6. **problem+json content-type на ошибках — работа реализации, не генератора.** Сервер отдал
   409 с `application/json` вместо `application/problem+json`: NSwag этого не контролирует,
   понадобится настройка на этапе E4 (фильтр/атрибут ProduceProblemDetails и т.п.).

## Найденные ограничения самого контракта (для E3/T8)

- **wall-clock datetime без offset** (решение T8) не имеет нативного скаляра TypeSpec: есть
  только `plainDate` (`format: date`) и `plainTime` (`format: time`). Spike моделирует слот как
  пару date+time — это проходит весь пайплайн (Kiota `Date`/`Time`, NSwag `DateTimeOffset`+
  `TimeSpan`). Реальному контракту (E3) надо явно выбрать форму.
- problem+json в TypeSpec 1.17 делается через literal-свойство ответа
  `@header("Content-Type") contentType: "application/problem+json"` + union
  `{ @statusCode, @body }`; `@produces`/`@contentType` декораторов нет.
- `@typespec/http-server-csharp` (альтернатива NSwag) в spike не участвовал — по R3 alpha и
  net9-hardcode; NSwag подтверждён как рабочий серверный генератор для .NET 10.
- Синтаксис 1.x: value-объекты в декораторах `#{}` (`@service(#{...})`), старые `{}`-примеры из
  0.x-доков не компилируются.

## Раскладка и размеры (для P2)

```
prototype/pipeline-spike/
  contracts/                     # TypeSpec-проект (package.json/lock, tspconfig.yaml, main.tsp)
    tsp-output/openapi/openapi.yaml   # 5.6K, committed
    node_modules/                # 45M — в git не попадает (.gitignore)
  src/SpikeClient/Generated/     # Kiota-выход: 12 .cs, ~900 строк, 55K + kiota-lock.json
  src/SpikeBlazor/               # razor-компонент, реально зовущий SDK (+ DI-фабрика паттерн)
  src/SpikeApi/                  # NSwag-выход Generated/Controllers.cs (365 строк, 17K)
    Generated/                   #   partial-контроллеры + I*-Controller + DTO (regeneration-safe:
    Implementation/              #   ручные реализации живут ВНЕ Generated/)
  Spike.slnx                     # throwaway-решение; main-решение не трогается
```

Тайминги на этой машине (Linux, Node 22.22.1, dotnet 10.0.112): `npm install` (91 пакет) — 12с
первично; `tsp compile` — 42ms; `kiota generate` — ~1с; `nswag openapi2cscontroller` — ~0.7с;
`dotnet build` (4 проекта) — 10с cold / 2.7с warm; wire-прогон — ещё несколько секунд.
Вывод для P2: генерация дешёвая, коммитить артефакты или перегенеривать на сборке — выбор не по
скорости, а по удобству ревью/CI.

Отдельно для CI-гейта: и Kiota-, и NSwag-выход проходят `dotnet format --verify-no-changes` без
исключений (проверено на spike-проектах) — генерированный код можно держать в основном sln-контурие
без подавлений.

## Как воспроизвести

    ./spike.sh

Нужны (уже установлены глобально на машине spike):

    dotnet tool install --global Microsoft.OpenApi.Kiota --version 1.36.1
    dotnet tool install --global NSwag.ConsoleCore --version 14.7.1

Замечание окружения: в WSL-деревьях с `/mnt/d` (drvfs) `npm install` создаёт `.bin`-симлинки и
падает с EPERM — spike-скрипт ставит `--no-bin-links` и зовёт компилятор через
`node node_modules/@typespec/compiler/cmd/tsp.js`. На нативном Linux (CI) этого нет.

Wire-проверка (spike.sh шаг 4) печатает: GET список enum'ов, GET availability с
`date=2026-03-05`, POST 201 с эхо date/time, POST 409 как `ApiException: status=409`
(`ProblemDetails`-наследник — Kiota делает из error-схем типизированные исключения).

## Что делать с этим spike нельзя

Не переиспользовать `/spike/`-маршруты и псевдо-модели: настоящий контракт пишет E3 по T1–T8,
настоящая раскладка и «одна команда» — P2 и E5. Ветка нужна только как реакция и факты.
