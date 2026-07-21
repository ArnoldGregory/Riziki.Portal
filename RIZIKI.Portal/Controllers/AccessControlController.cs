// ============================================================
//  RIZIKI.Portal — AccessControlController
//  Roles, Permissions, Role-Permissions, Portal Users
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class AccessControlController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;

        public AccessControlController(ApiClient api, AuditService audit)
        {
            _api = api;
            _audit = audit;
        }

        // ── Views ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> RolesIndex()
        {
            await _audit.LogViewAsync("AccessControl/Roles");
            return View();
        }

        public async Task<IActionResult> RolesUnapproved()
        {
            await _audit.LogViewAsync("AccessControl/RolesUnapproved");
            return View();
        }

        public async Task<IActionResult> Permissions()
        {
            await _audit.LogViewAsync("AccessControl/Permissions");
            return View();
        }

        public async Task<IActionResult> RolePermissions()
        {
            await _audit.LogViewAsync("AccessControl/RolePermissions");
            return View();
        }

        public async Task<IActionResult> PortalUsersIndex()
        {
            await _audit.LogViewAsync("AccessControl/Users");
            return View();
        }

        public async Task<IActionResult> PortalUsersUnapproved()
        {
            await _audit.LogViewAsync("AccessControl/UsersUnapproved");
            return View();
        }

        public async Task<IActionResult> PortalUserRoles()
        {
            await _audit.LogViewAsync("AccessControl/UserRoles");
            return View();
        }

        // Menu Access — placeholder until feature is fully designed
        public IActionResult MenuAccess() => View();

        // ── ROLES data actions ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var r = await _api.GetAsync<object>("api/AccessControl/GetRoles");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SaveRole([FromBody] RoleRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.role_name))
                return Json(new { success = false, message = "role_name is required" });

            var r = await _api.PostAsync<object>("api/AccessControl/CreateRole", new
            {
                id        = model.id,
                role_name = model.role_name.Trim()
            });
            return Json(r.IsSuccess
                ? new { success = true, message = model.id > 0 ? "Role updated" : "Role created" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRole([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/AccessControl/ApproveRole", new { id = model.id });
            return Json(r.IsSuccess
                ? new { success = true, message = "Role approved" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/AccessControl/Delete", new { id = model.id, module = "roles" });
            return Json(r.IsSuccess
                ? new { success = true, message = "Role deleted" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Cannot delete — may be in use" : r.Error });
        }

        // ── PERMISSIONS data actions ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetPermissions()
        {
            var r = await _api.GetAsync<object>("api/AccessControl/GetPermissions");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SavePermission([FromBody] PermissionRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.permission_name))
                return Json(new { success = false, message = "permission_name is required" });

            var r = await _api.PostAsync<object>("api/AccessControl/CreatePermission", new
            {
                permission_name = model.permission_name.Trim()
            });
            return Json(r.IsSuccess
                ? new { success = true, message = "Permission created" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePermission([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/AccessControl/Delete", new { id = model.id, module = "permissions" });
            return Json(r.IsSuccess
                ? new { success = true, message = "Permission deleted" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Cannot delete — may be in use" : r.Error });
        }

        // ── ROLE-PERMISSIONS data actions ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetRolePermissions(int role_id)
        {
            if (role_id <= 0) return Json(new List<object>());
            var r = await _api.GetAsync<object>($"api/AccessControl/GetRolePermissions?role_id={role_id}");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> AssignPermission([FromBody] AssignPermRequest model)
        {
            if (model == null) return Json(new { success = false, message = "Invalid request" });

            var r = await _api.PostAsync<object>("api/AccessControl/AssignPermissionToRole", new
            {
                mode          = model.mode,
                role_id       = model.role_id,
                permission_id = model.permission_id,
                id            = model.id
            });
            return Json(r.IsSuccess
                ? new { success = true, message = model.mode == "allocate" ? "Permission assigned" : "Permission removed" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── USERS data actions ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var r = await _api.GetAsync<object>("api/users/getusers");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SaveUser([FromBody] UserRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.email))
                return Json(new { success = false, message = "email is required" });

            if (model.id > 0)
            {
                // Update existing
                var ru = await _api.PostAsync<object>("api/users/updateuser", new
                {
                    id         = model.id,
                    first_name = model.first_name,
                    last_name  = model.last_name,
                    email      = model.email,
                    mobile     = model.mobile,
                    role_id    = model.role_id
                });
                return Json(ru.IsSuccess
                    ? new { success = true, message = "User updated" }
                    : new { success = false, message = string.IsNullOrEmpty(ru.Error) ? "Failed to update" : ru.Error });
            }

            // Create new
            if (string.IsNullOrWhiteSpace(model.password))
                return Json(new { success = false, message = "password is required" });

            var rc = await _api.PostAsync<object>("api/users/adduser", new
            {
                first_name  = model.first_name,
                last_name   = model.last_name,
                email       = model.email,
                mobile      = model.mobile,
                password    = model.password,
                role_id     = model.role_id,
                client_type = "CLIENT",
                avatar      = "user.jpg"
            });
            return Json(rc.IsSuccess
                ? new { success = true, message = "User created" }
                : new { success = false, message = string.IsNullOrEmpty(rc.Error) ? "Failed to create" : rc.Error });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/users/approveuser", new { id = model.id });
            return Json(r.IsSuccess
                ? new { success = true, message = "User approved" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> LockUser([FromBody] LockRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/users/lockuser", new { id = model.id, locked = model.locked });
            return Json(r.IsSuccess
                ? new { success = true, message = model.locked ? "User locked" : "User unlocked" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] IdRequest model)
        {
            if (model == null || model.id <= 0)
                return Json(new { success = false, message = "id is required" });

            var r = await _api.PostAsync<object>("api/users/deleteuser", new { id = model.id });
            return Json(r.IsSuccess
                ? new { success = true, message = "User deleted" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── Request models ────────────────────────────────────────────────────
        public class RoleRequest
        {
            public long    id        { get; set; }
            public string? role_name { get; set; }
        }
        public class PermissionRequest
        {
            public string? permission_name { get; set; }
        }
        public class IdRequest
        {
            public long id { get; set; }
        }
        public class AssignPermRequest
        {
            public string mode          { get; set; } = "allocate";
            public int    role_id       { get; set; }
            public int    permission_id { get; set; }
            public long   id            { get; set; }
        }
        public class LockRequest
        {
            public long id     { get; set; }
            public bool locked { get; set; }
        }
        public class UserRequest
        {
            public long    id         { get; set; }
            public string? first_name { get; set; }
            public string? last_name  { get; set; }
            public string? email      { get; set; }
            public string? mobile     { get; set; }
            public string? password   { get; set; }
            public int     role_id    { get; set; }
        }
        public class HRUserRequest
        {
            public string?   first_name    { get; set; }
            public string?   last_name     { get; set; }
            public string?   email         { get; set; }
            public string?   mobile        { get; set; }
            public string?   password      { get; set; }
            public long      role_id       { get; set; }
            public long      department_id { get; set; }
            public string?   staff_number  { get; set; }
            public string?   job_title     { get; set; }
            public DateTime? hire_date     { get; set; }
            public decimal   basic_salary  { get; set; }
        }

        // ── GET next auto staff number ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetNextStaffNumber()
        {
            var r = await _api.GetAsync<object>("api/employees/nextstaffnumber");
            return Json(r.IsSuccess ? r.Data : "EMP001");
        }

        // ── SAVE HR USER (creates portal user + employee record) ─────────────
        [HttpPost]
        public async Task<IActionResult> SaveHRUser([FromBody] HRUserRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.email))
                return Json(new { success = false, message = "email is required" });
            if (string.IsNullOrWhiteSpace(model.password))
                return Json(new { success = false, message = "password is required" });

            var r = await _api.PostAsync<object>("api/users/addhruser", new
            {
                first_name    = model.first_name,
                last_name     = model.last_name,
                email         = model.email,
                mobile        = model.mobile,
                password      = model.password,
                role_id       = model.role_id,
                department_id = model.department_id,
                staff_number  = model.staff_number,
                job_title     = model.job_title,
                hire_date     = model.hire_date,
                basic_salary  = model.basic_salary
            });
            return Json(r.IsSuccess
                ? new { success = true,  message = "HR user created" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }
    }
}
