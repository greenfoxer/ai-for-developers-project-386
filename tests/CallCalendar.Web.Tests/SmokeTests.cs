using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CallCalendar.Web.Tests;

public class SmokeTests
{
    [Fact]
    public async Task App_StartsAndResponds()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Landing_ShowsHeroAndLinksToStubPages()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("Calendar</h1>", html);
        Assert.Contains("href=\"book\"", html);
        Assert.Contains("href=\"admin\"", html);
    }

    [Fact]
    public async Task BookingStub_RespondsWithHeading()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/book");

        Assert.Contains("Запись на звонок", html);
    }

    [Fact]
    public async Task EventsStub_RespondsWithHeading()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/admin");

        Assert.Contains("Предстоящие события", html);
    }

    [Fact]
    public async Task BookingDegrades_WhenApiUnavailable()
    {
        // Заведомо недоступный base URL: порт 9 (discard) пуст — соединение отклоняется.
        // Проверка опирается на статический пререндер InteractiveServer-компонента:
        // без него в первом HTML не было бы состояния деградации.
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("CallCalendarApi:BaseUrl", "http://127.0.0.1:9"));
        using var client = factory.CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/book"));

        Assert.Contains("Запись на звонок", html);
        Assert.Contains("Сервис записи временно недоступен", html);
    }
}
