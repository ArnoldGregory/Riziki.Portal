// ============================================================
//  RIZIKI.Portal — DashboardController
//  Place in: Controllers/DashboardController.cs
//  One entry point; renders HR/Admin dashboard or Employee dashboard
//  based on profile_id (role_id) claim. 1/2 = HR/Admin, 3 = Employee.
// ============================================================

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AuditService _audit;
        private readonly ApiClient _api;

        public DashboardController(AuditService audit, ApiClient api)
        {
            _audit = audit;
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            await _audit.LogViewAsync("Dashboard");

            var roleId = User.FindFirst("profile_id")?.Value ?? "";
            var name = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var company = User.FindFirst("company_id")?.Value ?? "";

            ViewBag.Name = name;
            ViewBag.CompanyId = company;
            ViewBag.RoleId = roleId;

            // Employee (3) gets the employee dashboard; HR(2)/Admin(1) get the HR dashboard.
            if (roleId == "3")
            {
                ViewData["Title"] = "My Dashboard";
                return View("EmployeeDashboard");
            }

            ViewData["Title"] = "Dashboard";
            ViewBag.IsAdmin = roleId == "1";
            return View("HrDashboard");
        }

        // ── GET /Dashboard/HrAnalytics — full analytics payload ──────────────────
        [HttpGet]
        public async Task<IActionResult> HrAnalytics()
        {
            var r = await _api.GetAsync<object>("api/dashboard/hrstats");
            return Json(r.IsSuccess ? r.Data : null);
        }

        // ── GET /Dashboard/EmpAnalytics — full analytics payload ─────────────────
        [HttpGet]
        public async Task<IActionResult> EmpAnalytics()
        {
            var r = await _api.GetAsync<object>("api/dashboard/empstats");
            return Json(r.IsSuccess ? r.Data : null);
        }

        // ── GET /Dashboard/EmpStats — JSON stats for the Employee dashboard widgets ──
        [HttpGet]
        public async Task<IActionResult> EmpStats()
        {
            try
            {
                var balanceTask  = _api.GetAsync<List<JsonElement>>("api/me/leave/mybalance");
                var leaveTask    = _api.GetAsync<List<JsonElement>>("api/me/leave/myleave");
                var advTask      = _api.GetAsync<List<JsonElement>>("api/me/advance/myadvances");
                var payslipTask  = _api.GetAsync<List<JsonElement>>("api/me/mypayslips");

                await Task.WhenAll(balanceTask, leaveTask, advTask, payslipTask);

                // Sum remaining leave days across all leave types
                int leaveBalance = 0;
                if (balanceTask.Result.IsSuccess && balanceTask.Result.Data != null)
                {
                    foreach (var b in balanceTask.Result.Data)
                    {
                        foreach (var prop in b.EnumerateObject())
                        {
                            if (prop.Name.Equals("remaining_days", StringComparison.OrdinalIgnoreCase))
                            {
                                if (prop.Value.TryGetInt32(out int rd)) leaveBalance += rd;
                                break;
                            }
                        }
                    }
                }

                // Count pending leave requests
                int pendingLeave = 0;
                if (leaveTask.Result.IsSuccess && leaveTask.Result.Data != null)
                {
                    foreach (var l in leaveTask.Result.Data)
                    {
                        foreach (var prop in l.EnumerateObject())
                        {
                            if (prop.Name.Equals("status", StringComparison.OrdinalIgnoreCase))
                            {
                                if (prop.Value.GetString()?.Equals("PENDING", StringComparison.OrdinalIgnoreCase) == true)
                                    pendingLeave++;
                                break;
                            }
                        }
                    }
                }

                // Count total advances
                int advances = advTask.Result.IsSuccess && advTask.Result.Data != null
                    ? advTask.Result.Data.Count : 0;

                // Latest payslip net pay
                string latestPayslip = "—";
                if (payslipTask.Result.IsSuccess && payslipTask.Result.Data?.Count > 0)
                {
                    var latest = payslipTask.Result.Data[0];
                    foreach (var prop in latest.EnumerateObject())
                    {
                        if (prop.Name.Equals("net_pay", StringComparison.OrdinalIgnoreCase))
                        {
                            if (prop.Value.TryGetDecimal(out decimal np))
                                latestPayslip = "KES " + np.ToString("N2");
                            break;
                        }
                    }
                }

                return Json(new { leaveBalance, pendingLeave, advances, latestPayslip });
            }
            catch
            {
                return Json(new { leaveBalance = "—", pendingLeave = "—", advances = "—", latestPayslip = "—" });
            }
        }

        // ── GET /Dashboard/HrStats — JSON stats for the HR dashboard widgets ──
        [HttpGet]
        public async Task<IActionResult> HrStats()
        {
            try
            {
                // Run all 3 fast calls in parallel
                var empTask    = _api.GetAsync<List<JsonElement>>("api/employees/getemployees");
                var leaveTask  = _api.GetAsync<List<JsonElement>>("api/employees/allleave?status=PENDING");
                var advTask    = _api.GetAsync<List<JsonElement>>("api/employees/alladvances?status=PENDING");
                var periodTask = _api.GetAsync<List<JsonElement>>("api/payroll/periods");

                await Task.WhenAll(empTask, leaveTask, advTask, periodTask);

                int employees = empTask.Result.IsSuccess   && empTask.Result.Data   != null ? empTask.Result.Data.Count   : 0;
                int leave     = leaveTask.Result.IsSuccess && leaveTask.Result.Data != null ? leaveTask.Result.Data.Count : 0;
                int advances  = advTask.Result.IsSuccess   && advTask.Result.Data   != null ? advTask.Result.Data.Count   : 0;

                // Last payroll: take the most recent PROCESSED period and sum net_pay across its payslips
                string lastNet = "—";
                if (periodTask.Result.IsSuccess && periodTask.Result.Data?.Count > 0)
                {
                    // find the period with the highest id (try period_id then id)
                    long bestId = 0;
                    foreach (var p in periodTask.Result.Data)
                    {
                        // Only consider PROCESSED periods for last net
                        bool isProcessed = false;
                        long pid = 0;
                        foreach (var prop in p.EnumerateObject())
                        {
                            if (prop.Name.Equals("status", StringComparison.OrdinalIgnoreCase) &&
                                prop.Value.GetString()?.Equals("PROCESSED", StringComparison.OrdinalIgnoreCase) == true)
                                isProcessed = true;

                            if (prop.Name.Equals("period_id", StringComparison.OrdinalIgnoreCase) ||
                                prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                            {
                                if (prop.Value.TryGetInt64(out long v)) pid = v;
                            }
                        }
                        if (isProcessed && pid > bestId) bestId = pid;
                    }

                    if (bestId > 0)
                    {
                        var slips = await _api.GetAsync<List<JsonElement>>($"api/payroll/periodpayslips?period_id={bestId}");
                        if (slips.IsSuccess && slips.Data?.Count > 0)
                        {
                            decimal total = 0;
                            foreach (var slip in slips.Data)
                            {
                                foreach (var prop in slip.EnumerateObject())
                                {
                                    if (prop.Name.Equals("net_pay", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (prop.Value.TryGetDecimal(out decimal np)) total += np;
                                        break;
                                    }
                                }
                            }
                            lastNet = "KES " + total.ToString("N2");
                        }
                    }
                }

                return Json(new { employees, leave, advances, lastNet });
            }
            catch
            {
                return Json(new { employees = "—", leave = "—", advances = "—", lastNet = "—" });
            }
        }
    }
}