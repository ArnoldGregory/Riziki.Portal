// ============================================================
//  RIZIKI.Portal — SettingsController
//  Parameters, Banks, Bank Branches
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public SettingsController(ApiClient api, AuditService audit)
        {
            _api   = api;
            _audit = audit;
        }

        // ── Views ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Parameters()
        {
            await _audit.LogViewAsync("Settings/Parameters");
            return View();
        }

        public async Task<IActionResult> Banks()
        {
            await _audit.LogViewAsync("Settings/Banks");
            return View();
        }

        public async Task<IActionResult> BankBranches()
        {
            await _audit.LogViewAsync("Settings/BankBranches");
            return View();
        }

        // Departments lives in DepartmentsController — redirect there
        public IActionResult Departments() => RedirectToAction("Index", "Departments");

        public async Task<IActionResult> PublicHolidays()
        {
            await _audit.LogViewAsync("Settings/PublicHolidays");
            return View();
        }

        // ── Holiday data ──────────────────────────────────────────────────────
        [HttpGet] public async Task<IActionResult> GetHolidays()
        {
            var r = await _api.GetAsync<object>("api/settings/holidays");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }
        [HttpPost] public async Task<IActionResult> AddHoliday([FromBody] HolidayRequest m)
        {
            var r = await _api.PostAsync<object>("api/settings/addholiday", m);
            return Json(r.IsSuccess ? new { success = true, message = "Holiday added" } : new { success = false, message = r.Error ?? "Failed" });
        }
        [HttpPost] public async Task<IActionResult> DeleteHoliday([FromBody] IdRequest m)
        {
            var r = await _api.PostAsync<object>("api/settings/delete", new { id = m.id, module = "public_holiday" });
            return Json(r.IsSuccess ? new { success = true, message = "Deleted" } : new { success = false, message = r.Error ?? "Failed" });
        }

        public async Task<IActionResult> Company()
        {
            await _audit.LogViewAsync("Settings/Company");
            return View();
        }

        // ── PARAMETERS data ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetParameters()
        {
            var r = await _api.GetAsync<object>("api/settings/getparameters");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SaveParameter([FromBody] ParameterRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.item_key))
                return Json(new { success = false, message = "item_key is required" });

            var r = await _api.PostAsync<object>("api/settings/saveparameter", new
            {
                id         = model.id,
                item_key   = model.item_key.Trim(),
                item_value = model.item_value?.Trim() ?? "",
                comments   = model.comments
            });
            return Json(r.IsSuccess
                ? new { success = true, message = model.id > 0 ? "Parameter updated" : "Parameter created" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteParameter([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/settings/delete", new { id = model.id, module = "parameters" });
            return Json(r.IsSuccess
                ? new { success = true, message = "Parameter deleted" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── BANKS data ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetBanks()
        {
            var r = await _api.GetAsync<object>("api/settings/getbanks");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SaveBank([FromBody] BankRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.bank_name))
                return Json(new { success = false, message = "bank_name is required" });

            var r = await _api.PostAsync<object>("api/settings/savebank", new
            {
                id           = model.id,
                bank_code    = model.bank_code?.Trim() ?? "",
                bank_name    = model.bank_name.Trim(),
                abbreviation = model.abbreviation
            });
            return Json(r.IsSuccess
                ? new { success = true, message = model.id > 0 ? "Bank updated" : "Bank created" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBank([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/settings/delete", new { id = model.id, module = "banks" });
            return Json(r.IsSuccess
                ? new { success = true, message = "Bank deleted" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── BANK BRANCHES data ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetBankBranches(long bank_id = 0)
        {
            var r = await _api.GetAsync<object>($"api/settings/getbankbranches?bank_id={bank_id}");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SaveBankBranch([FromBody] BranchRequest model)
        {
            if (model == null || model.bank_id <= 0)
                return Json(new { success = false, message = "bank_id is required" });

            var r = await _api.PostAsync<object>("api/settings/savebankbranch", new
            {
                id          = model.id,
                bank_id     = model.bank_id,
                branch_code = model.branch_code?.Trim() ?? "",
                branch_name = model.branch_name?.Trim() ?? "",
                location    = model.location
            });
            return Json(r.IsSuccess
                ? new { success = true, message = model.id > 0 ? "Branch updated" : "Branch created" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBankBranch([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/settings/delete", new { id = model.id, module = "bank_branches" });
            return Json(r.IsSuccess
                ? new { success = true, message = "Branch deleted" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── COMPANY data ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetCompany()
        {
            var r = await _api.GetAsync<object>("api/settings/getcompany");
            return Json(r.IsSuccess ? r.Data : null);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCompany([FromBody] CompanyRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.company_name))
                return Json(new { success = false, message = "Company name is required" });

            var r = await _api.PostAsync<object>("api/settings/savecompany", new
            {
                company_name    = model.company_name.Trim(),
                company_address = model.company_address,
                company_phone   = model.company_phone,
                company_email   = model.company_email,
                kra_pin         = model.kra_pin,
                nssf_no         = model.nssf_no,
                nhif_no         = model.nhif_no
            });
            return Json(r.IsSuccess
                ? new { success = true,  message = "Company settings saved" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── POLICY view + data ────────────────────────────────────────────────
        public async Task<IActionResult> Policy()
        {
            await _audit.LogViewAsync("Settings/Policy");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicy()
        {
            var r = await _api.GetAsync<object>("api/settings/getpolicy");
            return Json(r.IsSuccess ? r.Data : null);
        }

        [HttpPost]
        public async Task<IActionResult> SavePolicy([FromBody] PolicyRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request" });

            var r = await _api.PostAsync<object>("api/settings/savepolicy", model);
            return Json(r.IsSuccess
                ? new { success = true,  message = "Policy saved successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to save policy" : r.Error });
        }

        // ── Request models ────────────────────────────────────────────────────
        public class PolicyRequest
        {
            public int     annual_leave_days       { get; set; }
            public int     sick_leave_days         { get; set; }
            public int     maternity_leave_days    { get; set; }
            public int     paternity_leave_days    { get; set; }
            public int     carry_forward_limit     { get; set; }
            public int     approval_levels         { get; set; }
            public int     require_payroll_approval{ get; set; }
            public decimal overtime_rate           { get; set; }
        }
        public class CompanyRequest
        {
            public string? company_name    { get; set; }
            public string? company_address { get; set; }
            public string? company_phone   { get; set; }
            public string? company_email   { get; set; }
            public string? kra_pin         { get; set; }
            public string? nssf_no         { get; set; }
            public string? nhif_no         { get; set; }
        }
        public class ParameterRequest
        {
            public long    id         { get; set; }
            public string? item_key   { get; set; }
            public string? item_value { get; set; }
            public string? comments   { get; set; }
        }
        public class BankRequest
        {
            public long    id            { get; set; }
            public string? bank_code     { get; set; }
            public string? bank_name     { get; set; }
            public string? abbreviation  { get; set; }
        }
        public class BranchRequest
        {
            public long    id          { get; set; }
            public long    bank_id     { get; set; }
            public string? branch_code { get; set; }
            public string? branch_name { get; set; }
            public string? location    { get; set; }
        }
        public class IdRequest        { public long id { get; set; } }
        public class HolidayRequest   { public string? holiday_name { get; set; } public DateTime holiday_date { get; set; } }
        public class NoticeRequest    { public string? title { get; set; } public string? content { get; set; } public string? target_role { get; set; } public DateTime? expiry_date { get; set; } }
        public class NoticeDeleteReq  { public long id { get; set; } }

        // ── BIOMETRIC DEVICES ─────────────────────────────────────────────────

        public async Task<IActionResult> Devices()
        {
            await _audit.LogViewAsync("Settings/Devices");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var r = await _api.GetAsync<object>("api/attendance/devices");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.device_name))
                return Json(new { success = false, message = "device_name is required" });
            if (string.IsNullOrWhiteSpace(model.device_serial))
                return Json(new { success = false, message = "device_serial is required" });

            var r = await _api.PostAsync<object>("api/attendance/devices/register", new
            {
                device_name   = model.device_name.Trim(),
                device_serial = model.device_serial.Trim(),
                location      = model.location
            });
            return Json(r.IsSuccess
                ? new { success = true,  message = "Device registered", data = r.Data }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error, data = (object?)null });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDevice([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/attendance/devices/delete", new { id = model.id });
            return Json(r.IsSuccess
                ? new { success = true,  message = "Device removed" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetDeviceEmployees()
        {
            var r = await _api.GetAsync<object>("api/attendance/employees");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFingerprintId([FromBody] FingerprintAssignRequest model)
        {
            if (model == null || model.employee_id <= 0)
                return Json(new { success = false, message = "employee_id is required" });

            var r = await _api.PostAsync<object>("api/attendance/employees/fingerprint", new
            {
                employee_id    = model.employee_id,
                fingerprint_id = model.fingerprint_id ?? ""
            });
            return Json(r.IsSuccess
                ? new { success = true,  message = "Fingerprint ID saved" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        public class DeviceRequest
        {
            public string? device_name   { get; set; }
            public string? device_serial { get; set; }
            public string? location      { get; set; }
        }
        public class FingerprintAssignRequest
        {
            public long    employee_id    { get; set; }
            public string? fingerprint_id { get; set; }
        }
    }
}
