using CallCalendar.Client;
using CallCalendar.Web.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Клиентский SDK из контракта: generated-код живёт в CallCalendar.Client (решение P2, #17).
builder.Services.AddCallCalendarApiClient(
    new Uri(builder.Configuration["CallCalendarApi:BaseUrl"] ?? "http://localhost:5000"));

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Behind reverse proxy (Railway) don't force HSTS redirects
    // app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Behind reverse proxy, https redirection can be problematic
// app.UseHttpsRedirection();

app.UseAntiforgery();

var staticFileOptions = new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".js"] = "application/javascript",
            [".mjs"] = "application/javascript",
            [".map"] = "application/json"
        }
    }
};
app.UseStaticFiles(staticFileOptions);
// app.MapStaticAssets(); // behind proxy can cause integrity/MIME issues; fallback to UseStaticFiles
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
