using FinTrack.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class CalendarController : Controller
{
    private readonly ICalendarService _calendar;

    public CalendarController(ICalendarService calendar)
    {
        _calendar = calendar;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        ViewData["Title"] = "Calendário";
        return View(await _calendar.GetMonthAsync(y, m));
    }

    [HttpGet]
    public async Task<IActionResult> Events(int year, int month)
    {
        var model = await _calendar.GetMonthAsync(year, month);
        return Json(new
        {
            year = model.Year,
            month = model.Month,
            monthLabel = model.MonthLabel,
            income = model.Income,
            expenses = model.Expenses,
            events = model.Events.Select(e => new
            {
                day = e.Day, title = e.Title, subtitle = e.Subtitle, tone = e.Tone,
                icon = e.Icon, url = e.Url, amount = e.Amount, kind = e.Kind
            })
        });
    }
}
