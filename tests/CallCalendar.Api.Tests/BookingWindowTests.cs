using System.Net;

namespace CallCalendar.Api.Tests;

/// Окно записи (T3): 14 календарных дней, включая сегодня; прошедшие старты отсекаются
/// строго после now (MSK); выходные не исключаются.
public class BookingWindowTests
{
    [Fact]
    public async Task Thirteenth_day_ahead_is_within_window()
    {
        using var app = new ApiTestApp();

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, ApiTestApp.Day(13));

        Assert.NotEmpty(slots);
    }

    [Fact]
    public async Task Fourteenth_day_ahead_is_out_of_window()
    {
        using var app = new ApiTestApp();

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, ApiTestApp.Day(14));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task Past_day_offers_nothing()
    {
        using var app = new ApiTestApp();

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, ApiTestApp.Day(-1));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task Today_offers_only_starts_strictly_after_now()
    {
        using var app = new ApiTestApp();
        var nowWall = ApiTestApp.NowWall();

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, ApiTestApp.Day(0));

        // Временной интервал узкий (runner-часы), но любой предложенный старт — строго «после now».
        foreach (var slot in slots)
        {
            var parsed = DateTime.ParseExact(slot, ApiTestApp.WallFormat, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(parsed > nowWall, $"слот {slot} не позже now {nowWall:O}");
        }
    }

    [Fact]
    public async Task Saturday_is_a_working_day()
    {
        using var app = new ApiTestApp();
        // Первый будущий выходной (суббота или воскресенье) — тоже должен иметь слоты.
        var day = ApiTestApp.Day(1);
        while (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
        {
            day = day.AddDays(1);
        }
        Assert.True((day - ApiTestApp.NowWall().Date).TotalDays < 14, "выходной выпал за окно — тест не показателен");

        var slots = await app.AvailabilityAsync(ApiTestApp.ThirtyMinTypeId, day);

        Assert.NotEmpty(slots);
    }
}
