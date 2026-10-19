using Microsoft.AspNetCore.Mvc;

namespace CallCalendar.Spike.Api.Implementation;

/// <summary>
/// THROWAWAY (ticket #16): стаб-реализация сгенерированного IEventTypesController —
/// демо-данные, показывающие, что partial-контроллер и ручная реализация разведены.
/// Реализация НЕ наследуется от ControllerBase, поэтому результаты создаются напрямую.
/// </summary>
public sealed class EventTypesImpl : IEventTypesController
{
    public Task<ActionResult<ICollection<EventType>>> ListAsync()
    {
        ICollection<EventType> items =
        [
            new EventType { Id = 1, Name = "call-30", Kind = EventTypeKind.Call, DurationMinutes = 30 },
            new EventType { Id = 2, Name = "meeting-60", Kind = EventTypeKind.Meeting, DurationMinutes = 60, Description = "длинная встреча" },
        ];
        return Task.FromResult<ActionResult<ICollection<EventType>>>(new OkObjectResult(items));
    }

    public Task<ActionResult<ICollection<Slot>>> AvailabilityAsync(int eventTypeId, DateTimeOffset date)
    {
        ICollection<Slot> slots =
        [
            new Slot { Date = date, Start = new TimeSpan(10, 30, 0), Available = true },
            new Slot { Date = date, Start = new TimeSpan(11, 0, 0), Available = false },
        ];
        return Task.FromResult<ActionResult<ICollection<Slot>>>(new OkObjectResult(slots));
    }
}

/// <summary>
/// THROWAWAY (ticket #16): стаб IBookingsController. Старт 10:30 возвращает 409 —
/// проверка, что ActionResult сохраняет код статуса, а Kiota маппит ошибку в ProblemDetails.
/// </summary>
public sealed class BookingsImpl : IBookingsController
{
    public Task<ActionResult<Booking>> CreateAsync(BookingCreate body)
    {
        if (body.Start == new TimeSpan(10, 30, 0))
        {
            var problem = new ProblemDetails
            {
                Type = "https://spike.example/conflict",
                Title = "Conflict",
                Status = 409,
                Detail = "слот занят (демо конфликта T2)",
            };
            return Task.FromResult<ActionResult<Booking>>(new ConflictObjectResult(problem));
        }

        var booking = new Booking
        {
            Id = 42,
            EventTypeId = body.EventTypeId,
            Date = body.Date,
            Start = body.Start,
            Guest = body.Guest,
        };
        return Task.FromResult<ActionResult<Booking>>(new CreatedResult("/spike/bookings/42", booking));
    }
}
