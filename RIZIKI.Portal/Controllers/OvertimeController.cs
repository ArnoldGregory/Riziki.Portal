// ============================================================
//  RIZIKI.Portal — OvertimeController (HR side)
//  Routes:
//    GET  /Overtime/Pending       → pending overtime view
//    GET  /Overtime/GetAll        → JSON all overtime (filtered by status)
//    POST /Overtime/Approve       → api/employees/reviewovertime (APPROVE)
//    POST /Overtime/Reject        → api/employees/reviewovertime (REJECT)
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class OvertimeController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public OvertimeController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        public async Task<IActionResult> Pending()
        {
            await _audit.LogViewAsync("Overtime/Pending");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string status = "")
        {
            var result = await _api.GetAsync<object>($"api/employees/allovertime?status={Uri.EscapeDataString(status)}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> Approve([FromBody] DecisionRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });

            var result = await _api.PostAsync<object>("api/employees/reviewovertime",
                new { request_id = model.request_id, action = "APPROVE", reject_reason = (string?)null });

            return Json(result.IsSuccess
                ? new { success = true, message = "Overtime approved" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to approve" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> Reject([FromBody] DecisionRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });

            var result = await _api.PostAsync<object>("api/employees/reviewovertime",
                new { request_id = model.request_id, action = "REJECT", reject_reason = model.reject_reason });

            return Json(result.IsSuccess
                ? new { success = true, message = "Overtime rejected" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to reject" : result.Error });
        }

        public class DecisionRequest { public long request_id { get; set; } public string? reject_reason { get; set; } }
    }
}
