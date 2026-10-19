using CallCalendar.Spike.Client;
using CallCalendar.Spike.Client.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

// THROWAWAY PROTOTYPE (ticket #16): сквозная wire-проверка цепочки
// Kiota-клиент (STJ) -> HTTP -> NSwag-сервер (Newtonsoft/MVC).
// Ожидание: GET-списки, GET с date-параметром, POST 201, POST 409 (ProblemDetails).

var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: new HttpClient());
var client = new CallCalendarClient(adapter);

Console.WriteLine("== GET /spike/event-types ==");
var events = await client.Spike.EventTypes.GetAsync();
foreach (var e in events ?? [])
{
    Console.WriteLine($"  {e.Id} {e.Name} kind={e.Kind} dur={e.DurationMinutes}min");
}

Console.WriteLine("== GET /spike/event-types/availability?eventTypeId=1&date=2026-03-05 ==");
var slots = await client.Spike.EventTypes.Availability.GetAsync(c =>
{
    c.QueryParameters.EventTypeId = 1;
    c.QueryParameters.Date = new Date(2026, 3, 5);
});
foreach (var s in slots ?? [])
{
    Console.WriteLine($"  slot date={s.Date} start={s.Start} available={s.Available}");
}

Console.WriteLine("== POST /spike/bookings (ожидание 201) ==");
var created = await client.Spike.Bookings.PostAsync(new BookingCreate
{
    EventTypeId = 1,
    Date = new Date(2026, 3, 6),
    Start = new Time(12, 0, 0),
    Guest = new GuestInfo { Name = "Иван", Email = "ivan@example.test" },
});
Console.WriteLine($"  created id={created?.Id} date={created?.Date} start={created?.Start} guest={created?.Guest?.Name}");

Console.WriteLine("== POST /spike/bookings (ожидание 409 -> Kiota error mapping) ==");
try
{
    await client.Spike.Bookings.PostAsync(new BookingCreate
    {
        EventTypeId = 1,
        Date = new Date(2026, 3, 5),
        Start = new Time(10, 30, 0),
        Guest = new GuestInfo { Name = "Пётр", Email = "petr@example.test" },
    });
    Console.WriteLine("  UNEXPECTED: исключения не было");
}
catch (ApiException ex)
{
    Console.WriteLine($"  ApiException: type={ex.GetType().Name} status={ex.ResponseStatusCode} msg={ex.Message}");
}

Console.WriteLine("SPIKE RUN COMPLETE");
