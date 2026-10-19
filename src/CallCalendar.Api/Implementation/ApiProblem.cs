using Microsoft.AspNetCore.Mvc;

namespace CallCalendar.Api.Implementation;

// Единый конструктор ошибок application/problem+json (T8 Q3): контент-тип — забота
// реализации, не NSwag-генератора (наблюдение P1, #16).
internal static class ApiProblem
{
    public static ActionResult Create(int statusCode, string title, string detail) =>
        new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://callcalendar.test/errors/{statusCode}",
        })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
}
