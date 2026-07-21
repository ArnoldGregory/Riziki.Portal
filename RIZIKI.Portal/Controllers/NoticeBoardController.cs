using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class NoticeBoardController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public NoticeBoardController(ApiClient api, AuditService audit) { _api = api; _audit = audit; }

        // HR — manage notices
        public async Task<IActionResult> Index()
        {
            await _audit.LogViewAsync("NoticeBoard/Index");
            return View();
        }

        // Employee — view notices
        public async Task<IActionResult> MyNotices()
        {
            await _audit.LogViewAsync("NoticeBoard/MyNotices");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNotices(string role = "ALL")
        {
            var r = await _api.GetAsync<object>($"api/notices?role={role}");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> AddNotice([FromBody] NoticeRequest m)
        {
            var r = await _api.PostAsync<object>("api/notices/add", m);
            return Json(r.IsSuccess ? new { success = true, message = "Notice posted" } : new { success = false, message = r.Error ?? "Failed" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotice([FromBody] IdRequest m)
        {
            var r = await _api.PostAsync<object>("api/notices/delete", new { id = m.id });
            return Json(r.IsSuccess ? new { success = true, message = "Deleted" } : new { success = false, message = r.Error ?? "Failed" });
        }

        public class NoticeRequest  { public string? title { get; set; } public string? content { get; set; } public string? target_role { get; set; } public DateTime? expiry_date { get; set; } }
        public class IdRequest      { public long id { get; set; } }
    }
}
