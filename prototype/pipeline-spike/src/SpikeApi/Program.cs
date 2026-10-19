using CallCalendar.Spike.Api;
using CallCalendar.Spike.Api.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// THROWAWAY PROTOTYPE (wayfinder ticket #16).
var builder = WebApplication.CreateBuilder(args);

// NSwag-DTO используют Newtonsoft-атрибуты (StringEnumConverter, DateFormatConverter):
// без Newtonsoft-форматтеров дефолтный STJ сломает wire-формат enum/date.
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddSingleton<IEventTypesController, EventTypesImpl>();
builder.Services.AddSingleton<IBookingsController, BookingsImpl>();

var app = builder.Build();
app.MapControllers();
app.Run("http://localhost:5071");
