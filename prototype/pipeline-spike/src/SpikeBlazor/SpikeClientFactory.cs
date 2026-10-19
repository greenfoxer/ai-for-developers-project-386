using CallCalendar.Spike.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace SpikeBlazor;

/// <summary>
/// THROWAWAY (ticket #16): DI-паттерн подключения Kiota-клиента (по туториалу R2:
/// зарегистрированный HttpClient + адаптер + фабрика). В реальном Blazor-приложении
/// это живёт в Program.cs: services.AddHttpClient&lt;...&gt;(...).
/// </summary>
public static class SpikeClientFactory
{
    public static CallCalendarClient Create(HttpClient httpClient)
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
        return new CallCalendarClient(adapter);
    }
}
