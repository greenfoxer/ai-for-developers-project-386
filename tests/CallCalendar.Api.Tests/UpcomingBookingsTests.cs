using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CallCalendar.Api.Tests;

/// Предстоящие встречи (T7/T6): все типы одним списком, старт строго после now (MSK),
/// по возрастанию; конец производный; плоские поля гостя, включая доп. информацию.
public class UpcomingBookingsTests
{
    [Fact]
    public async Task Upcoming_lists_future_bookings_sorted_by_start()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);
        var inTwoDays = ApiTestApp.Day(2);

        Assert.Equal(HttpStatusCode.Created, (await app.BookAsync(ApiTestApp.SixtyMinTypeId, inTwoDays, 9, 0, additionalInfo: "обсудить roadmap")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 15, 0)).StatusCode);

        var upcoming = await app.Client.GetFromJsonAsync<List<Booking>>("/api/bookings", ApiTestApp.Json);

        Assert.Equal(2, upcoming!.Count);
        // Сортировка по возрастанию старта: завтра раньше послезавтра.
        Assert.Equal(ApiTestApp.Wall(tomorrow, 15, 0), upcoming[0].Start);
        Assert.Equal(ApiTestApp.Wall(inTwoDays, 9, 0), upcoming[1].Start);
    }

    [Fact]
    public async Task Booking_response_echoes_wall_clock_and_derives_end_from_type_duration()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        var response = await app.BookAsync(ApiTestApp.SixtyMinTypeId, tomorrow, 10, 0, name: "Иван", email: "ivan@example.test", additionalInfo: "перезвоните за 5 минут");
        var booking = await response.Content.ReadFromJsonAsync<Booking>(ApiTestApp.Json);

        Assert.Equal(ApiTestApp.Wall(tomorrow, 10, 0), booking!.Start);
        Assert.Equal(ApiTestApp.Wall(tomorrow, 11, 0), booking.End); // 60-мин тип
        Assert.Equal("Иван", booking.Name);
        Assert.Equal("ivan@example.test", booking.Email);
        Assert.Equal("перезвоните за 5 минут", booking.AdditionalInfo);
        Assert.Equal("Часовая встреча", booking.EventTypeName);
    }
}
