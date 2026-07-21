using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class AppraisalController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public AppraisalController(ApiClient api, AuditService audit) { _api = api; _audit = audit; }

        // HR — manage cycles
        public async Task<IActionResult> Cycles()
        {
            await _audit.LogViewAsync("Appraisal/Cycles");
            return View();
        }

        // HR — set goals & rate employees for a cycle
        public async Task<IActionResult> Review(long cycle_id = 0)
        {
            await _audit.LogViewAsync("Appraisal/Review");
            ViewBag.CycleId = cycle_id;
            return View();
        }

        // Employee — view goals & submit self-assessment
        public async Task<IActionResult> MyGoals()
        {
            await _audit.LogViewAsync("Appraisal/MyGoals");
            return View();
        }

        [HttpGet] public async Task<IActionResult> GetCycles()
        {
            var r = await _api.GetAsync<object>("api/appraisal/cycles");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost] public async Task<IActionResult> CreateCycle([FromBody] CycleRequest m)
        {
            var r = await _api.PostAsync<object>("api/appraisal/createcycle", m);
            return Json(r.IsSuccess ? new { success = true, message = "Cycle created" } : new { success = false, message = r.Error ?? "Failed" });
        }

        [HttpPost] public async Task<IActionResult> CloseCycle([FromBody] IdRequest m)
        {
            var r = await _api.PostAsync<object>("api/appraisal/closecycle", new { id = m.id });
            return Json(r.IsSuccess ? new { success = true, message = "Cycle closed" } : new { success = false, message = r.Error ?? "Failed" });
        }

        [HttpGet] public async Task<IActionResult> GetGoals(long cycle_id, long employee_id = 0)
        {
            var r = await _api.GetAsync<object>($"api/appraisal/goals?cycle_id={cycle_id}&employee_id={employee_id}");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpGet] public async Task<IActionResult> GetEmployees()
        {
            var r = await _api.GetAsync<object>("api/employees/getemployees");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost] public async Task<IActionResult> AddGoal([FromBody] GoalRequest m)
        {
            var r = await _api.PostAsync<object>("api/appraisal/addgoal", m);
            return Json(r.IsSuccess ? new { success = true, message = "Goal added" } : new { success = false, message = r.Error ?? "Failed" });
        }

        [HttpPost] public async Task<IActionResult> SelfRate([FromBody] SelfRateRequest m)
        {
            var r = await _api.PostAsync<object>("api/appraisal/selfrate", m);
            return Json(r.IsSuccess ? new { success = true, message = "Submitted" } : new { success = false, message = r.Error ?? "Failed" });
        }

        [HttpPost] public async Task<IActionResult> ManagerRate([FromBody] ManagerRateRequest m)
        {
            var r = await _api.PostAsync<object>("api/appraisal/managerrate", m);
            return Json(r.IsSuccess ? new { success = true, message = "Rating saved" } : new { success = false, message = r.Error ?? "Failed" });
        }

        public class CycleRequest       { public string? cycle_name { get; set; } public DateTime start_date { get; set; } public DateTime end_date { get; set; } }
        public class GoalRequest        { public long cycle_id { get; set; } public long employee_id { get; set; } public string? goal_title { get; set; } public string? goal_description { get; set; } public decimal weight { get; set; } }
        public class SelfRateRequest    { public long appraisal_id { get; set; } public decimal self_rating { get; set; } public string? self_comments { get; set; } }
        public class ManagerRateRequest { public long appraisal_id { get; set; } public decimal manager_rating { get; set; } public string? manager_comments { get; set; } }
        public class IdRequest          { public long id { get; set; } }
    }
}
