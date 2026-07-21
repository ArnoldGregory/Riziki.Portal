

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public DepartmentsController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            await _audit.LogViewAsync("Departments");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var result = await _api.GetAsync<object>("api/departments/departments");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment([FromBody] DeptRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.department_name))
                return Json(new { success = false, message = "Department name is required" });

            var result = await _api.PostAsync<object>("api/departments/adddepartment",
                new { department_name = model.department_name, description = model.description });

            return Json(result.IsSuccess
                ? new { success = true, message = "Department added" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to add department" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> EditDepartment([FromBody] DeptRequest model)
        {
            if (model == null || model.id <= 0 || string.IsNullOrWhiteSpace(model.department_name))
                return Json(new { success = false, message = "id and department_name are required" });

            var result = await _api.PostAsync<object>("api/departments/updatedepartment",
                new { id = model.id, department_name = model.department_name, description = model.description });

            return Json(result.IsSuccess
                ? new { success = true, message = "Department updated" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to update department" : result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var result = await _api.PostAsync<object>("api/departments/deletedepartment", new { id = model.id });

            return Json(result.IsSuccess
                ? new { success = true, message = "Department deleted" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to delete department" : result.Error });
        }

        public class DeptRequest
        {
            public long   id              { get; set; }
            public string? department_name { get; set; }
            public string? description     { get; set; }
        }

        public class IdRequest { public long id { get; set; } }
    }
}
