// ============================================================
//  RIZIKI.Portal — LeaveController (HR side)
//  Routes:
//    GET  /Leave/Pending           → pending leave approvals view
//    GET  /Leave/Types             → leave types view
//    GET  /Leave/GetAll            → JSON all leave (optionally filtered by status)
//    GET  /Leave/GetTypes          → JSON leave types
//    POST /Leave/Approve           → api/employees/approveleave
//    POST /Leave/Reject            → api/employees/rejectleave
//    POST /Leave/AddType           → api/employees/addleavetype
//    POST /Leave/DeleteType        → api/employees/deleteleavetype
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public LeaveController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        public async Task<IActionResult> Pending()
        {
            await _audit.LogViewAsync("Leave/Pending");
            return View();
        }

        public async Task<IActionResult> Types()
        {
            await _audit.LogViewAsync("Leave/Types");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string status = "")
        {
            var result = await _api.GetAsync<object>($"api/employees/allleave?status={Uri.EscapeDataString(status)}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetTypes()
        {
            var result = await _api.GetAsync<object>("api/employees/leavetypes");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> Approve([FromBody] DecisionRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });

            var result = await _api.PostAsync<object>("api/employees/approveleave",
                new { request_id = model.request_id, reject_reason = (string?)null });

            return Json(result.IsSuccess
                ? new { success = true, message = "Leave approved" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to approve" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> Reject([FromBody] DecisionRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });

            var result = await _api.PostAsync<object>("api/employees/rejectleave",
                new { request_id = model.request_id, reject_reason = model.reject_reason });

            return Json(result.IsSuccess
                ? new { success = true, message = "Leave rejected" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to reject" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> AddType([FromBody] LeaveTypeRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.leave_type_name))
                return Json(new { success = false, message = "Leave type name is required" });

            var result = await _api.PostAsync<object>("api/employees/addleavetype",
                new { leave_type_name = model.leave_type_name, annual_days = model.annual_days, description = model.description });

            return Json(result.IsSuccess
                ? new { success = true, message = "Leave type added" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to add leave type" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteType([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var result = await _api.PostAsync<object>("api/employees/deleteleavetype",
                new { request_id = model.id });

            return Json(result.IsSuccess
                ? new { success = true, message = "Leave type deleted" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to delete" : result.Error });
        }

        // ── Year-end carry forward ─────────────────────────────────────────────
        public async Task<IActionResult> YearEnd()
        {
            await _audit.LogViewAsync("Leave/YearEnd");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCFLog()
        {
            var r = await _api.GetAsync<object>("api/settings/getcflog");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> RunCarryForward([FromBody] CarryForwardRequest model)
        {
            if (model == null || model.year <= 0)
                return Json(new { success = false, message = "year is required" });

            var r = await _api.PostAsync<object>("api/settings/carryforward",
                new { year = model.year, max_carry = model.max_carry });

            return Json(r.IsSuccess
                ? new { success = true,  message = r.Data?.ToString() ?? "Carry forward processed" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        public class DecisionRequest    { public long request_id { get; set; } public string? reject_reason { get; set; } }
        public class LeaveTypeRequest   { public string? leave_type_name { get; set; } public int annual_days { get; set; } public string? description { get; set; } }
        public class IdRequest          { public long id { get; set; } }
        public class CarryForwardRequest{ public int year { get; set; } public decimal max_carry { get; set; } = 10; }
    }
}
