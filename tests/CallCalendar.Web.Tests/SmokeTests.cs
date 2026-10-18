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
}
