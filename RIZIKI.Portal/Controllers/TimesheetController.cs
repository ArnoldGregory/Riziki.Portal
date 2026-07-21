// ============================================================
//  RIZIKI.Portal — TimesheetController
//  Routes: /Timesheet/*
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;
using System.Security.Claims;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class TimesheetController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public TimesheetController(ApiClient api, AuditService audit)
        {
            _api   = api;
            _audit = audit;
        }

        // ── CALENDAR VIEW ─────────────────────────────────────────────
        public async Task<IActionResult> Calendar(int? year = null, int? month = null)
        {
            await _audit.LogViewAsync("Timesheet/Calendar");

            var now          = DateTime.Now;
            var displayYear  = year  ?? now.Year;
            var displayMonth = month ?? now.Month;

            if (displayMonth < 1 || displayMonth > 12) displayMonth = now.Month;
            if (displayYear < 2020 || displayYear > now.Year + 1) displayYear = now.Year;

            var firstDay = new DateOnly(displayYear, displayMonth, 1);
            var lastDay  = firstDay.AddMonths(1).AddDays(-1);

            var result = await _api.GetAsync<List<TimesheetEntryDto>>(
                $"api/timesheet/myentries?from={firstDay:yyyy-MM-dd}&to={lastDay:yyyy-MM-dd}");

            var entries = result.IsSuccess ? result.Data ?? new() : new();

            var dayGroups = entries
                .Where(e => e.started_at.HasValue)
                .GroupBy(e => DateOnly.FromDateTime(e.started_at!.Value))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var dayEntries   = g.ToList();
                        var totalMinutes = dayEntries.Sum(e => e.duration_minutes ?? 0);
                        return new TimesheetDayGroup
                        {
                            Date         = g.Key,
                            Entries      = dayEntries,
                            TotalMinutes = totalMinutes,
                            TotalDisplay = FormatDuration(totalMinutes)
                        };
                    });

            var vm = new TimesheetCalendarVm
            {
                Year          = displayYear,
                Month         = displayMonth,
                Entries       = entries,
                DayGroups     = dayGroups,
                PreviousMonth = firstDay.AddDays(-1),
                NextMonth     = lastDay.AddDays(1)
            };

            return View(vm);
        }

        // ── START TIMER ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> StartTimer([FromBody] StartTimerRequest model)
        {
            if (string.IsNullOrWhiteSpace(model?.activity_type))
                return Json(new { success = false, message = "Activity type is required" });

            var r = await _api.PostAsync<object>("api/timesheet/start", new
            {
                activity_type = model.activity_type,
                description   = model.description,
                started_at    = model.started_at,
                notes         = model.notes
            });

            if (!r.IsSuccess)
                return Json(new { success = false, message = r.Error ?? "Failed to start activity" });

            // Extract new ID from data
            var raw  = r.Data?.ToString() ?? "{}";
            var json = System.Text.Json.JsonDocument.Parse(raw);
            long timesheetId = 0;
            try { timesheetId = json.RootElement.GetProperty("id").GetInt64(); } catch { }

            return Json(new { success = true, message = "Activity started", timesheetId });
        }

        // ── STOP TIMER ────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> StopTimer([FromBody] StopTimerRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/timesheet/stop", new
            {
                id       = model.id,
                ended_at = model.ended_at,
                notes    = model.notes
            });

            return Json(r.IsSuccess
                ? new { success = true,  message = "Activity stopped" }
                : new { success = false, message = r.Error ?? "Failed to stop activity" });
        }

        // ── GET ACTIVE ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetActiveTimer()
        {
            var r = await _api.GetAsync<object>("api/timesheet/active");
            return Json(r.IsSuccess ? r.Data : null);
        }

        // ── HR SUMMARY VIEW ───────────────────────────────────────────
        public async Task<IActionResult> Summary()
        {
            await _audit.LogViewAsync("Timesheet/Summary");
            return View();
        }

        // ── HR SUMMARY DATA ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetSummary(string? from = null, string? to = null)
        {
            var url = "api/timesheet/summary";
            if (!string.IsNullOrWhiteSpace(from) || !string.IsNullOrWhiteSpace(to))
                url += $"?from={from ?? ""}&to={to ?? ""}";

            var r = await _api.GetAsync<object>(url);
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        // ── HELPER ───────────────────────────────────────────────────
        private static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return "0m";
            var h = minutes / 60; var m = minutes % 60;
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }

        // ── REQUEST MODELS ────────────────────────────────────────────
        public class StartTimerRequest
        {
            public string?   activity_type { get; set; }
            public string?   description   { get; set; }
            public DateTime? started_at    { get; set; }
            public string?   notes         { get; set; }
        }
        public class StopTimerRequest
        {
            public long      id       { get; set; }
            public DateTime? ended_at { get; set; }
            public string?   notes    { get; set; }
        }
    }

    // ── DTOs & VIEW MODELS ────────────────────────────────────────────
    public class TimesheetEntryDto
    {
        public long      id               { get; set; }
        public long      employee_id      { get; set; }
        public string?   employee_name    { get; set; }
        public string?   activity_type    { get; set; }
        public string?   activity_display { get; set; }
        public string?   description      { get; set; }
        public DateTime? started_at       { get; set; }
        public DateTime? ended_at         { get; set; }
        public int?      duration_minutes { get; set; }
        public string?   duration_display { get; set; }
        public string?   notes            { get; set; }
        public bool      is_running       { get; set; }
    }

    public class TimesheetCalendarVm
    {
        public int                                    Year          { get; set; }
        public int                                    Month         { get; set; }
        public List<TimesheetEntryDto>                Entries       { get; set; } = new();
        public Dictionary<DateOnly, TimesheetDayGroup> DayGroups    { get; set; } = new();
        public DateOnly                               PreviousMonth { get; set; }
        public DateOnly                               NextMonth     { get; set; }
    }

    public class TimesheetDayGroup
    {
        public DateOnly               Date         { get; set; }
        public List<TimesheetEntryDto> Entries     { get; set; } = new();
        public int                    TotalMinutes { get; set; }
        public string                 TotalDisplay { get; set; } = "";
    }
}
