using System.Net;
using System.Net.Http.Json;

namespace CallCalendar.Api.Tests;

/// Семантика занятости (T2): полуоткрытые интервалы [start, start+duration);
/// стык в стык разрешён, пересечение на минуту и более — отказ; проигравший в гонке — 409.
public class BookingConflictTests
{
    [Fact]
    public async Task Back_to_back_bookings_are_allowed()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        Assert.Equal(HttpStatusCode.Created, (await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 10, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 10, 30)).StatusCode);
    }

    [Fact]
    public async Task Exact_same_slot_is_rejected_with_409()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        Assert.Equal(HttpStatusCode.Created, (await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 11, 0)).StatusCode);
        var second = await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 11, 0);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Overlap_across_different_event_types_is_rejected_with_409()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        // 60-минутная запись 12:00–13:00 блокирует и слоты другого типа внутри интервала.
        Assert.Equal(HttpStatusCode.Created, (await app.BookAsync(ApiTestApp.SixtyMinTypeId, tomorrow, 12, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 12, 30)).StatusCode);
    }

    [Fact]
    public async Task Two_guests_cannot_take_the_same_slot_under_contention()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        // Гонка (T2): два конкурента на один слот — ровно один успех, проигравший не второй 201.
        var first = app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 13, 0);
        var second = app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 13, 0);
        var responses = await Task.WhenAll(first, second);

        var codes = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();
        Assert.Equal(1, codes.Count(c => c == HttpStatusCode.Created));
        Assert.DoesNotContain(HttpStatusCode.InternalServerError, codes);
    }

    [Fact]
    public async Task Conflict_response_is_problem_plus_json_with_full_body()
    {
        using var app = new ApiTestApp();
        var tomorrow = ApiTestApp.Day(1);

        await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 14, 0);
        var response = await app.BookAsync(ApiTestApp.ThirtyMinTypeId, tomorrow, 14, 0);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestApp.Json);
        Assert.Equal(409, problem!.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
        Assert.False(string.IsNullOrWhiteSpace(problem.Type));
    }
}
