using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CallCalendar.Api.Tests;

/// Срез 1 (шов): API поднимается, сид-демо типы событий читаются через контрактный маршрут.
public class EventTypesReadTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Visitor_can_list_event_types_seeded_for_demo()
    {
        using var app = new ApiTestApp();

        var response = await app.Client.GetAsync("/api/event-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var types = await response.Content.ReadFromJsonAsync<List<EventType>>(Json);
        Assert.NotNull(types);
        Assert.Equal(2, types.Count);
        Assert.Contains(types, t => t.Name == "30-минутный созвон" && t.DurationMinutes == 30);
        Assert.Contains(types, t => t.Name == "Часовая встреча" && t.DurationMinutes == 60);
    }
}
