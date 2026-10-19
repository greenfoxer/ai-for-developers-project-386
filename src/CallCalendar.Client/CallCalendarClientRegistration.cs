using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace CallCalendar.Client;

/// DI-подключение клиентского SDK (паттерн проверен spike'ем #16: именованный HttpClient + адаптер).
public static class CallCalendarClientServiceCollectionExtensions
{
    public static IServiceCollection AddCallCalendarApiClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient(nameof(CallCalendarClient), client => client.BaseAddress = baseAddress);
        services.AddScoped(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CallCalendarClient));
            var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            return new CallCalendarClient(adapter);
        });
        return services;
    }
}
