// ============================================================
//  RIZIKI.Portal — EmployeesController
//  Routes:
//    GET  /Employees/Index           → employee list + add modal
//    GET  /Employees/Add             → redirects to Index
//    GET  /Employees/Attendance      → HR attendance view
//    GET  /Employees/GetList         → JSON proxy → api/employees/getemployees
//    GET  /Employees/GetDepartments  → JSON proxy → api/departments/departments
//    GET  /Employees/GetAttendance?employee_id=&from_date=&to_date= → proxy
//    POST /Employees/AddEmployee     → proxy → api/employees/addemployee
//    GET  /Employees/DownloadDocument?doc_id= → proxy → api/employees/downloaddocument
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public EmployeesController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        // ── GET /Employees/Index ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            await _audit.LogViewAsync("Employees");
            return View();
        }

        // ── GET /Employees/Add → redirect to Index (modal lives there) ───────
        public IActionResult Add() => RedirectToAction(nameof(Index));

        // ── GET /Employees/Attendance ────────────────────────────────────────
        public async Task<IActionResult> Attendance()
        {
            await _audit.LogViewAsync("Employees/Attendance");
            return View();
        }

        // ── GET /Employees/GetAttendance ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAttendance(long employee_id = 0, string? from_date = null, string? to_date = null)
        {
            var qs = $"api/employees/allattendance?employee_id={employee_id}";
            if (!string.IsNullOrWhiteSpace(from_date)) qs += $"&from_date={from_date}";
            if (!string.IsNullOrWhiteSpace(to_date))   qs += $"&to_date={to_date}";
            var result = await _api.GetAsync<object>(qs);
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        // ── GET /Employees/GetList ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var result = await _api.GetAsync<object>("api/employees/getemployees");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        // ── GET /Employees/GetDepartments ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var result = await _api.GetAsync<object>("api/departments/departments");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        // ── GET /Employees/GetBanks ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetBanks()
        {
            var result = await _api.GetAsync<object>("api/settings/getbanks");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        // ── POST /Employees/AddEmployee ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request" });

            var result = await _api.PostAsync<object>("api/employees/addemployee", new
            {
                first_name          = model.first_name,
                middle_name         = model.middle_name,
                last_name           = model.last_name,
                id_number           = model.id_number,
                email               = model.email,
                mobile              = model.mobile,
                gender              = model.gender,
                date_of_birth       = model.date_of_birth,
                job_title           = model.job_title,
                employment_type     = model.employment_type,
                hire_date           = model.hire_date,
                department_id       = model.department_id,
                basic_salary        = model.basic_salary,
                kra_pin             = model.kra_pin,
                nssf_number         = model.nssf_number,
                nhif_number         = model.nhif_number,
                bank_id             = model.bank_id,
                bank_account_no     = model.bank_account_no,
                bank_branch         = model.bank_branch,
                payment_method      = model.payment_method ?? "BANK",
                mobile_money_number = model.mobile_money_number,
                password            = model.password,
                role_id             = 3
            });

            if (result.IsSuccess)
                return Json(new { success = true, message = "Employee added successfully" });

            return Json(new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to add employee" : result.Error });
        }

        // ── GET /Employees/DownloadDocument?doc_id= ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(long doc_id)
        {
            if (doc_id <= 0) return BadRequest("doc_id required");
            var (bytes, contentType, fileName, error) = await _api.GetFileAsync($"api/employees/downloaddocument?doc_id={doc_id}");
            if (bytes == null) return BadRequest(error ?? "File not found");
            return File(bytes, contentType ?? "application/octet-stream", fileName ?? $"document_{doc_id}");
        }

        // ── GET /Employees/GetDocuments?employee_id= ─────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDocuments(long employee_id)
        {
            if (employee_id <= 0) return Json(new List<object>());
            var result = await _api.GetAsync<object>($"api/employees/employeedocuments?employee_id={employee_id}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        // ── POST /Employees/UploadDocument ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromBody] UploadDocumentRequest model)
        {
            if (model == null || model.employee_id <= 0)
                return Json(new { success = false, message = "employee_id is required" });
            if (string.IsNullOrWhiteSpace(model.file_base64))
                return Json(new { success = false, message = "File is required" });
            if (string.IsNullOrWhiteSpace(model.file_name))
                return Json(new { success = false, message = "file_name is required" });

            var result = await _api.PostAsync<object>("api/employees/uploaddocument", new
            {
                employee_id   = model.employee_id,
                document_type = model.document_type,
                file_name     = model.file_name,
                file_base64   = model.file_base64
            });

            return Json(result.IsSuccess
                ? new { success = true, message = "Document uploaded successfully" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to upload document" : result.Error });
        }

        // ── GET /Employees/GetDeductions?employee_id= ────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDeductions(long employee_id)
        {
            if (employee_id <= 0) return Json(new List<object>());
            var result = await _api.GetAsync<object>($"api/employees/deductions?employee_id={employee_id}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        // ── POST /Employees/AddDeduction ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AddDeduction([FromBody] DeductionRequest model)
        {
            if (model == null || model.employee_id <= 0)
                return Json(new { success = false, message = "employee_id is required" });

            var result = await _api.PostAsync<object>("api/employees/adddeduction", new
            {
                employee_id    = model.employee_id,
                deduction_name = model.deduction_name,
                amount         = model.amount,
                is_recurring   = model.is_recurring
            });

            return Json(result.IsSuccess
                ? new { success = true, message = "Deduction added" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to add deduction" : result.Error });
        }

        // ── POST /Employees/DeleteDeduction ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteDeduction([FromBody] DeductionDeleteRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var result = await _api.PostAsync<object>("api/employees/deletededuction", new { id = model.id });
            return Json(result.IsSuccess
                ? new { success = true, message = "Deduction removed" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to remove deduction" : result.Error });
        }

        // ── HOD ──────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SetHod([FromBody] SetHodRequest model)
        {
            if (model == null || model.employee_id <= 0)
                return Json(new { success = false, message = "employee_id is required" });
            var result = await _api.PostAsync<object>("api/employees/sethod", new
            {
                employee_id = model.employee_id,
                is_hod      = model.is_hod
            });
            return Json(result.IsSuccess
                ? new { success = true, message = "Done" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed" : result.Error });
        }

        // ── Request model ────────────────────────────────────────────────────
        public class SetHodRequest { public long employee_id { get; set; } public bool is_hod { get; set; } }
        public class DeductionRequest
        {
            public long    employee_id    { get; set; }
            public string? deduction_name { get; set; }
            public decimal amount         { get; set; }
            public bool    is_recurring   { get; set; }
        }
        public class DeductionDeleteRequest { public long id { get; set; } }

        public class UploadDocumentRequest
        {
            public long    employee_id   { get; set; }
            public string? document_type { get; set; }
            public string? file_name     { get; set; }
            public string? file_base64   { get; set; }
        }

        public class AddEmployeeRequest
        {
            public string? first_name           { get; set; }
            public string? middle_name          { get; set; }
            public string? last_name            { get; set; }
            public string? id_number            { get; set; }
            public string? email                { get; set; }
            public string? mobile               { get; set; }
            public string? gender               { get; set; }
            public string? date_of_birth        { get; set; }
            public string? job_title            { get; set; }
            public string? employment_type      { get; set; }
            public string? hire_date            { get; set; }
            public long    department_id        { get; set; }
            public decimal basic_salary         { get; set; }
            public string? kra_pin              { get; set; }
            public string? nssf_number          { get; set; }
            public string? nhif_number          { get; set; }
            public long    bank_id              { get; set; }
            public string? bank_account_no      { get; set; }
            public string? bank_branch          { get; set; }
            public string? payment_method       { get; set; }
            public string? mobile_money_number  { get; set; }
            public string? password             { get; set; }
        }
    }
}
