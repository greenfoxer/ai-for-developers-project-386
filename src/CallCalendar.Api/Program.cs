using CallCalendar.Api;
using CallCalendar.Api.Domain;
using CallCalendar.Api.Implementation;
using CallCalendar.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQLite + EF Core за интерфейсами сторов (T1). Схема — EnsureCreated для дев/демо;
// миграции — вопрос продуктовой фазы (решение E4, см. #19).
var connectionString = builder.Configuration.GetConnectionString("sqlite") ?? "Data Source=callcalendar.db";
builder.Services.AddDbContext<CallCalendarDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IEventTypeStore, EventTypeStore>();
builder.Services.AddScoped<IBookingStore, BookingStore>();

// NSwag-DTO полагаются на Newtonsoft-атрибуты (StringEnumConverter/DateFormatConverter) —
// форматтеры тоже Newtonsoft (правило P1, #16).
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddScoped<IEventTypesController, EventTypesImplementation>();
builder.Services.AddScoped<IBookingsController, BookingsImplementation>();

var app = builder.Build();

using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CallCalendarDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.EventTypes.AnyAsync())
    {
        // Дем-данные курса: два типа событий.
        db.EventTypes.AddRange(
            new EventTypeEntity { Name = "30-минутный созвон", Description = "Короткий звонок", DurationMinutes = 30 },
            new EventTypeEntity { Name = "Часовая встреча", DurationMinutes = 60 });
        await db.SaveChangesAsync();
    }
}

app.MapControllers();

// Dev-порт совпадает с @server контракта (http://localhost:5000, E3).
app.Run("http://localhost:5000");
