// ============================================================
//  RIZIKI.Portal — ReportsController (HR side)
//  Routes:
//    GET  /Reports/PayrollSummary   → payroll summary report view
//    GET  /Reports/Employees        → employee report view
//    GET  /Reports/Leave            → leave report view
//    GET  /Reports/GetPeriods       → JSON periods (for selector)
//    GET  /Reports/GetPayslips?period_id= → JSON payslips for period
//    GET  /Reports/GetEmployees     → JSON employee list
//    GET  /Reports/GetLeaveReport?status=&employee_id= → JSON leave records
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public ReportsController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        public async Task<IActionResult> PayrollSummary()
        {
            await _audit.LogViewAsync("Reports/PayrollSummary");
            return View();
        }

        public async Task<IActionResult> Employees()
        {
            await _audit.LogViewAsync("Reports/Employees");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPeriods()
        {
            var result = await _api.GetAsync<object>("api/payroll/periods");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetPayslips(long period_id)
        {
            if (period_id <= 0) return Json(new List<object>());
            var result = await _api.GetAsync<object>($"api/payroll/periodpayslips?period_id={period_id}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var result = await _api.GetAsync<object>("api/employees/getemployees");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        public async Task<IActionResult> Leave()
        {
            await _audit.LogViewAsync("Reports/Leave");
            return View();
        }

        public async Task<IActionResult> Attendance()
        {
            await _audit.LogViewAsync("Reports/Attendance");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(long employee_id = 0, string? from_date = null, string? to_date = null)
        {
            var qs = $"api/employees/allattendance?employee_id={employee_id}";
            if (!string.IsNullOrWhiteSpace(from_date)) qs += $"&from_date={from_date}";
            if (!string.IsNullOrWhiteSpace(to_date))   qs += $"&to_date={to_date}";
            var result = await _api.GetAsync<object>(qs);
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        public async Task<IActionResult> P9()
        {
            await _audit.LogViewAsync("Reports/P9");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetP9Data(long employee_id, int year = 0)
        {
            if (year <= 0) year = DateTime.Now.Year;
            var result = await _api.GetAsync<object>($"api/payroll/p9?employee_id={employee_id}&year={year}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        public async Task<IActionResult> StatutoryReturns()
        {
            await _audit.LogViewAsync("Reports/StatutoryReturns");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatutoryReturns(int year = 0)
        {
            if (year <= 0) year = DateTime.Now.Year;
            var result = await _api.GetAsync<object>($"api/reports/statutory?year={year}");
            if (!result.IsSuccess) return Json(new { success = false, data = new List<object>() });
            return Json(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStatutoryReturns(int year = 0)
        {
            if (year <= 0) year = DateTime.Now.Year;
            var (bytes, contentType, fileName, error) = await _api.GetFileAsync($"api/reports/statutory/excel?year={year}");
            if (bytes == null) return BadRequest(error ?? "Failed to generate statutory returns export");
            return File(bytes, contentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName ?? $"statutory_returns_{year}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaveReport(string? status, long employee_id = 0)
        {
            var qs = "api/employees/allleave";
            if (!string.IsNullOrWhiteSpace(status)) qs += $"?status={status}";
            var result = await _api.GetAsync<object>(qs);
            if (!result.IsSuccess) return Json(new List<object>());

            // If filtering by a specific employee, do it portal-side
            if (employee_id > 0 && result.Data is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var filtered = new List<object>();
                foreach (var item in je.EnumerateArray())
                {
                    foreach (var prop in item.EnumerateObject())
                    {
                        if ((prop.Name.Equals("employee_id", StringComparison.OrdinalIgnoreCase) ||
                             prop.Name.Equals("emp_id", StringComparison.OrdinalIgnoreCase)) &&
                            prop.Value.TryGetInt64(out long eid) && eid == employee_id)
                        {
                            filtered.Add(item);
                            break;
                        }
                    }
                }
                return Json(filtered);
            }
            return Json(result.Data);
        }
    }
}
