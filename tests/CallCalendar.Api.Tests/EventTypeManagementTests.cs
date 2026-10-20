using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CallCalendar.Api.Tests;

/// Типы событий (T5/T7): создание с проверками, просмотр, уникальность названия
/// без учёта регистра и обрамляющих пробелов.
public class EventTypeManagementTests
{
    [Fact]
    public async Task Owner_creates_event_type_and_sees_it_in_list()
    {
        using var app = new ApiTestApp();

        var created = await app.CreateEventTypeAsync("Двухчасовой воркшоп", 120, description: "Большая встреча");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var dto = await created.Content.ReadFromJsonAsync<EventType>(ApiTestApp.Json);
        Assert.Equal("Двухчасовой воркшоп", dto!.Name);
        Assert.Equal(120, dto.DurationMinutes);

        var types = await app.Client.GetFromJsonAsync<List<EventType>>("/api/event-types", ApiTestApp.Json);
        Assert.Contains(types!, t => t.Id == dto.Id && t.Description == "Большая встреча");
    }

    [Fact]
    public async Task Missing_body_is_400_not_500()
    {
        using var app = new ApiTestApp();

        var response = await app.Client.PostAsync("/api/event-types",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_name_ignoring_case_and_spaces_is_rejected_with_409()
    {
        using var app = new ApiTestApp();

        Assert.Equal(HttpStatusCode.Created, (await app.CreateEventTypeAsync("Уникальный тип", 45)).StatusCode);

        var duplicate = await app.CreateEventTypeAsync("  УНИКАЛЬНЫЙ тип  ", 45);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Blank_name_is_rejected_with_400()
    {
        using var app = new ApiTestApp();

        var response = await app.CreateEventTypeAsync("   ", 30);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Non_positive_duration_is_rejected_with_400()
    {
        using var app = new ApiTestApp();

        Assert.Equal(HttpStatusCode.BadRequest, (await app.CreateEventTypeAsync("Нулевой тип", 0)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.CreateEventTypeAsync("Отрицательный тип", -15)).StatusCode);
    }
}
