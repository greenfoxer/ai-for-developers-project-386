namespace CallCalendar.Api.Tests;

/// Сетка Слотов (T4): шаг = длительность Типа события, якорь 09:00, запись целиком
/// влезает в рабочий день до 18:00; один предикат свободы для availability и создания (T2).
public class SlotGridTests
{
    [Fact]
    public async Task Sixty_minute_type_offers_hourly_starts_from_0900_to_1700()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        var slots = await app.AvailabilityAsync(ApiTestApp.SixtyMinTypeId, tomorrow);

        // Независимая спецификация: 9 стартов с часовым шагом, последний — 17:00
        // (17:00+60 = 18:00 влезает, 18:00+60 — нет).
        string[] expected = ["09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00"];
        var date = tomorrow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected.Select(t => $"{date}T{t}"), slots);
    }

    [Fact]
    public async Task Thirty_minute_type_offers_half_hourly_starts_from_0900_to_1730()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, tomorrow);

        Assert.Equal(18, slots.Count);
        Assert.Equal(ApiTestApp.Wall(tomorrow, 9, 0), slots[0]);
        Assert.Equal(ApiTestApp.Wall(tomorrow, 17, 30), slots[^1]);
    }

    [Fact]
    public async Task Booked_start_disappears_from_availability_single_predicate()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        var created = await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 10, 0);
        Assert.True(created.StatusCode == System.Net.HttpStatusCode.Created, await created.Content.ReadAsStringAsync());

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, tomorrow);

        Assert.DoesNotContain(ApiTestApp.Wall(tomorrow, 10, 0), slots);
        Assert.Contains(ApiTestApp.Wall(tomorrow, 9, 30), slots); // сосед не задет
    }
}
