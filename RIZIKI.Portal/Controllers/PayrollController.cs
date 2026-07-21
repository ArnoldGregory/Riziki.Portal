// ============================================================
//  RIZIKI.Portal — PayrollController (HR side)
//  Routes:
//    GET  /Payroll/Salaries          → salary structures view
//    GET  /Payroll/Periods           → payroll periods view
//    GET  /Payroll/GetEmployees      → JSON employee list
//    GET  /Payroll/GetSalaryStructure?employee_id= → JSON salary
//    POST /Payroll/SetSalary         → api/payroll/setsalary
//    GET  /Payroll/GetPeriods        → JSON periods list
//    POST /Payroll/CreatePeriod      → api/payroll/createperiod
//    POST /Payroll/RunPayroll        → api/payroll/runpayroll
//    GET  /Payroll/GetPayslips?period_id= → JSON payslips
//    GET  /Payroll/BankFile?period_id=   → streams CSV download
//    GET  /Payroll/PayslipPdf?payslip_id= → streams PDF download
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public PayrollController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        public async Task<IActionResult> Salaries()
        {
            await _audit.LogViewAsync("Payroll/Salaries");
            return View();
        }

        public async Task<IActionResult> Periods()
        {
            await _audit.LogViewAsync("Payroll/Periods");
            return View();
        }

        public async Task<IActionResult> Run()
        {
            await _audit.LogViewAsync("Payroll/Run");
            return View();
        }

        public async Task<IActionResult> Payslips()
        {
            await _audit.LogViewAsync("Payroll/Payslips");
            return View();
        }

        // ── Data endpoints ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var result = await _api.GetAsync<object>("api/employees/getemployees");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetSalaryStructure(long employee_id)
        {
            if (employee_id <= 0) return Json(new { success = false, message = "employee_id required" });
            var result = await _api.GetAsync<object>($"api/payroll/salarystructure?employee_id={employee_id}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
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

        // ── Actions ───────────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> SetSalary([FromBody] SetSalaryRequest model)
        {
            if (model == null || model.employee_id <= 0)
                return Json(new { success = false, message = "employee_id is required" });

            var result = await _api.PostAsync<object>("api/payroll/setsalary", model);
            return Json(result.IsSuccess
                ? new { success = true, message = "Salary structure saved" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to save salary" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePeriod([FromBody] CreatePeriodRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.period_name))
                return Json(new { success = false, message = "period_name is required" });

            var result = await _api.PostAsync<object>("api/payroll/createperiod", model);
            return Json(result.IsSuccess
                ? new { success = true, message = "Period created" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to create period" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> RunPayroll([FromBody] RunPayrollRequest model)
        {
            if (model == null || model.period_id <= 0)
                return Json(new { success = false, message = "period_id is required" });

            var result = await _api.PostAsync<object>("api/payroll/runpayroll", model);
            return Json(result.IsSuccess
                ? new { success = true, message = "Payroll processed successfully", data = (object?)null }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to run payroll" : result.Error, data = (object?)null });
        }

        // ── File downloads ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BankFile(long period_id)
        {
            if (period_id <= 0) return BadRequest("period_id required");
            var (bytes, contentType, fileName, error) = await _api.GetFileAsync($"api/payroll/bankfile?period_id={period_id}");
            if (bytes == null) return BadRequest(error ?? "Failed to download bank file");
            return File(bytes, contentType ?? "text/csv", fileName ?? $"bankfile_{period_id}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> PayslipPdf(long payslip_id)
        {
            if (payslip_id <= 0) return BadRequest("payslip_id required");
            var (bytes, contentType, fileName, error) = await _api.GetFileAsync($"api/payroll/payslippdf?payslip_id={payslip_id}");
            if (bytes == null) return BadRequest(error ?? "Failed to generate payslip PDF");
            return File(bytes, contentType ?? "application/pdf", fileName ?? $"payslip_{payslip_id}.pdf");
        }

        // ── Request models ────────────────────────────────────────────────────

        public class SetSalaryRequest
        {
            public long    employee_id          { get; set; }
            public decimal basic_salary         { get; set; }
            public decimal house_allowance      { get; set; }
            public decimal transport_allowance  { get; set; }
            public decimal medical_allowance    { get; set; }
            public decimal other_allowance      { get; set; }
            public string? effective_date       { get; set; }
        }

        public class CreatePeriodRequest
        {
            public string? period_name   { get; set; }
            public string? start_date    { get; set; }
            public string? end_date      { get; set; }
            public string? payment_date  { get; set; }
        }

        public class RunPayrollRequest  { public long period_id { get; set; } }
        public class LockPeriodRequest   { public long period_id { get; set; } }
        public class QuickRunRequest     { public int payment_day { get; set; } = 28; }

        [HttpPost]
        public async Task<IActionResult> LockPeriod([FromBody] LockPeriodRequest model)
        {
            var result = await _api.PostAsync<object>("api/payroll/lockperiod", model);
            return Json(result.IsSuccess
                ? new { success = true,  message = "Period locked successfully", data = result.Data }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to lock period" : result.Error, data = (object?)null });
        }

        [HttpPost]
        public async Task<IActionResult> QuickRun([FromBody] QuickRunRequest model)
        {
            var result = await _api.PostAsync<object>("api/payroll/quickrun", model);
            return Json(result.IsSuccess
                ? new { success = true,  message = "Payroll processed successfully", data = (object?)null }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to run payroll" : result.Error, data = (object?)null });
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePayroll([FromBody] PayrollReviewRequest model)
        {
            var result = await _api.PostAsync<object>("api/payroll/approvepayroll", model);
            return Json(result.IsSuccess
                ? new { success = true,  message = "Payroll approved and moved to PROCESSED" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to approve payroll" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> RejectPayroll([FromBody] PayrollReviewRequest model)
        {
            var result = await _api.PostAsync<object>("api/payroll/rejectpayroll", model);
            return Json(result.IsSuccess
                ? new { success = true,  message = "Payroll rejected and returned to OPEN" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to reject payroll" : result.Error });
        }

        public class PayrollReviewRequest { public long period_id { get; set; } public string? comments { get; set; } }
    }
}
