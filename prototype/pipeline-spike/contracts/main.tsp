// THROWAWAY PROTOTYPE (wayfinder #16): spike-контракт для проверки проходимости
// цепочки TypeSpec -> OpenAPI -> Kiota (client) -> NSwag (server). Не спецификация,
// а псевдо-контракт из 3 операций, намеренно покрывающий «опасные» формы API.

import "@typespec/http";
import "@typespec/openapi3";

using TypeSpec.Http;

// --- операции: 3, сгруппированы в интерфейсы (интерфейс -> OpenAPI tag -> отдельный контроллер NSwag) ---

@server("http://localhost:5071", "Spike dev server")
@service(#{ title: "CallCalendar Pipeline Spike" })
namespace CallCalendar.Spike;

@tag("EventTypes")
@route("/spike/event-types")
interface EventTypes {
    @doc("Список типов событий (GET, array ответ)")
    @get
    list(): EventType[];

    @doc("Доступность на дату (query-параметры, format: date)")
    @get
    @route("/availability")
    availability(@query eventTypeId: int32, @query date: plainDate): Slot[];
}

@tag("Bookings")
@route("/spike/bookings")
interface Bookings {
    @doc("Создание записи (body, 201 + 400/404/409 problem+json)")
    @post
    create(@body booking: BookingCreate):
      {
        @statusCode statusCode: 201;
        @body body: Booking;
      }
      | {
        @statusCode statusCode: 400;
        @header("Content-Type") contentType: "application/problem+json";
        @body body: ProblemDetails;
      }
      | {
        @statusCode statusCode: 404;
        @header("Content-Type") contentType: "application/problem+json";
        @body body: ProblemDetails;
      }
      | {
        @statusCode statusCode: 409;
        @header("Content-Type") contentType: "application/problem+json";
        @body body: ProblemDetails;
      };
}

// --- модели: риск-места ---

@doc("Псевдо-тип события (мотивы T5: int id, name required, optional description, duration > 0)")
model EventType {
  @doc("id — целое с автонумерацией (T5); непрозрачный ключ маршрута")
  id: int32;
  @minLength(1)
  name: string;
  description?: string;
  @doc("минуты, > 0 (T5)")
  @minValue(1)
  durationMinutes: int32;
  @doc("enum через pipeline — риск для NSwag/Kiota")
  kind: "call" | "meeting";
}

@doc("Псевдо-слот (мотивы T3/T4/T8): wall-clock date+time отдельными скалярами")
model Slot {
  // format: date — Kiota -> Date, NSwag -> DateTime: проверяем оба выхода
  date: plainDate;
  // format: time — самый рискованный скаляр пайплайна (нет нативного .NET-типа)
  start: plainTime;
  available: boolean;
}

@doc("Гость (T6: name+email required, additionalInfo optional)")
model GuestInfo {
  @minLength(1)
  name: string;
  @minLength(1)
  email: string;
  additionalInfo?: string;
}

@doc("Запись (T6): id/eventTypeId/start, конец производный от длительности")
model Booking {
  id: int32;
  eventTypeId: int32;
  date: plainDate;
  start: plainTime;
  guest: GuestInfo;
}

@doc("Создание записи (POST body)")
model BookingCreate {
  eventTypeId: int32;
  date: plainDate;
  start: plainTime;
  guest: GuestInfo;
}

// problem+json: медиа-тип ответа задаём константным @header-свойством в wrapper-модели ответа.
// (В @typespec/http 1.17 нет @produces/@contentType-декораторов — риск-место для T8.)
model ProblemDetails {
  type?: string;
  title?: string;
  @doc("http-код, int32")
  status?: int32;
  detail?: string;
}
