// ============================================================
//  RIZIKI.Portal — AdvancesController (HR side)
//  Routes:
//    GET  /Advances/Pending       → pending advances view
//    GET  /Advances/GetAll        → JSON all advances (filtered by status)
//    POST /Advances/Approve       → api/employees/approveadvance
//    POST /Advances/Reject        → api/employees/rejectadvance
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class AdvancesController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public AdvancesController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        public async Task<IActionResult> Pending()
        {
            await _audit.LogViewAsync("Advances/Pending");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string status = "")
        {
            var result = await _api.GetAsync<object>($"api/employees/alladvances?status={Uri.EscapeDataString(status)}");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> Approve([FromBody] DecisionRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });

            var result = await _api.PostAsync<object>("api/employees/approveadvance",
                new { request_id = model.request_id, reject_reason = (string?)null });

            return Json(result.IsSuccess
                ? new { success = true, message = "Advance approved" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to approve" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> Reject([FromBody] DecisionRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });

            var result = await _api.PostAsync<object>("api/employees/rejectadvance",
                new { request_id = model.request_id, reject_reason = model.reject_reason });

            return Json(result.IsSuccess
                ? new { success = true, message = "Advance rejected" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to reject" : result.Error });
        }

        public class DecisionRequest { public long request_id { get; set; } public string? reject_reason { get; set; } }
    }
}
