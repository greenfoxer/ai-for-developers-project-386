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
}
