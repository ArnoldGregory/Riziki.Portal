// ============================================================
//  RIZIKI.Portal — SelfServiceController  (Employee side)
//  All routes under /SelfService/
//  Proxies api/me/* endpoints (EmployeeSelfServiceController in API)
// ============================================================

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RIZIKI.Portal.Services;
using System.Security.Claims;

namespace RIZIKI.Portal.Controllers
{
    [Authorize]
    public class SelfServiceController : Controller
    {
        private readonly ApiClient _api;
        private readonly AuditService _audit;
        private readonly IWebHostEnvironment _env;

        public SelfServiceController(ApiClient api, AuditService audit, IWebHostEnvironment env)
        {
            _api = api;
            _audit = audit;
            _env = env;
        }

        // ── Views ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> MyLeaveRequests()
        {
            await _audit.LogViewAsync("SelfService/MyLeaveRequests");
            return View();
        }

        // "Apply for Leave" in the menu — same page as requests (modal lives there)
        public IActionResult ApplyForLeave() => RedirectToAction(nameof(MyLeaveRequests));

        public async Task<IActionResult> MyBalance()
        {
            await _audit.LogViewAsync("SelfService/MyBalance");
            return View();
        }

        public async Task<IActionResult> MyAdvances()
        {
            await _audit.LogViewAsync("SelfService/MyAdvances");
            return View();
        }

        public async Task<IActionResult> Attendance()
        {
            await _audit.LogViewAsync("SelfService/Attendance");
            return View();
        }

        public async Task<IActionResult> MyPayslips()
        {
            await _audit.LogViewAsync("SelfService/MyPayslips");
            return View();
        }

        public async Task<IActionResult> MyDocuments()
        {
            await _audit.LogViewAsync("SelfService/MyDocuments");
            return View();
        }

        public async Task<IActionResult> MyProfile()
        {
            await _audit.LogViewAsync("SelfService/MyProfile");
            return View();
        }

        // ── Data / action endpoints ───────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetMyLeave()
        {
            var r = await _api.GetAsync<object>("api/me/leave/myleave");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var r = await _api.GetAsync<object>("api/me/leave/types");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetMyBalance()
        {
            var r = await _api.GetAsync<object>("api/me/leave/mybalance");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveRequest model)
        {
            if (model == null || model.leave_type_id <= 0)
                return Json(new { success = false, message = "leave_type_id is required" });
            if (string.IsNullOrWhiteSpace(model.start_date) || string.IsNullOrWhiteSpace(model.end_date))
                return Json(new { success = false, message = "start_date and end_date are required" });

            var r = await _api.PostAsync<object>("api/me/leave/apply", new
            {
                leave_type_id   = model.leave_type_id,
                start_date      = DateTime.Parse(model.start_date),
                end_date        = model.is_half_day ? DateTime.Parse(model.start_date) : DateTime.Parse(model.end_date),
                reason          = model.reason,
                is_half_day     = model.is_half_day,
                half_day_period = model.half_day_period
            });
            return Json(r.IsSuccess
                ? new { success = true, message = "Leave request submitted successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to submit leave request" : r.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAdvances()
        {
            var r = await _api.GetAsync<object>("api/me/advance/myadvances");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> RequestAdvance([FromBody] AdvanceRequest model)
        {
            if (model == null || model.amount <= 0)
                return Json(new { success = false, message = "Amount must be greater than 0" });

            var r = await _api.PostAsync<object>("api/me/advance/request", new
            {
                amount = model.amount,
                reason = model.reason
            });
            return Json(r.IsSuccess
                ? new { success = true, message = "Advance request submitted successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to submit advance request" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> RecallLeave([FromBody] RecallRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });
            var r = await _api.PostAsync<object>("api/me/leave/recall", new { request_id = model.request_id });
            return Json(r.IsSuccess
                ? new { success = true,  message = "Leave request cancelled successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to cancel leave request" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> RecallAdvance([FromBody] RecallRequest model)
        {
            if (model == null || model.request_id <= 0)
                return Json(new { success = false, message = "request_id is required" });
            var r = await _api.PostAsync<object>("api/me/advance/recall", new { request_id = model.request_id });
            return Json(r.IsSuccess
                ? new { success = true,  message = "Advance request cancelled successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to cancel advance request" : r.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAttendance()
        {
            var r = await _api.GetAsync<object>("api/me/myattendance");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> ClockIn()
        {
            var r = await _api.PostAsync<object>("api/me/clockin", new { });
            return Json(r.IsSuccess
                ? new { success = true, message = "Clocked in successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to clock in" : r.Error });
        }

        [HttpPost]
        public async Task<IActionResult> ClockOut()
        {
            var r = await _api.PostAsync<object>("api/me/clockout", new { });
            return Json(r.IsSuccess
                ? new { success = true, message = "Clocked out successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to clock out" : r.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPayslips()
        {
            var r = await _api.GetAsync<object>("api/me/mypayslips");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetMyDocuments()
        {
            var r = await _api.GetAsync<object>("api/me/mydocuments");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var r = await _api.GetAsync<object>("api/me/myprofile");
            return Json(r.IsSuccess ? r.Data : null);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            var r = await _api.PostAsync<object>("api/me/updateprofile", new
            {
                mobile = model?.mobile,
                email  = model?.email
            });
            return Json(r.IsSuccess
                ? new { success = true, message = "Profile updated successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to update profile" : r.Error });
        }

        // ── Document download proxy ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(long doc_id)
        {
            if (doc_id <= 0) return BadRequest("doc_id required");
            var (bytes, contentType, fileName, error) = await _api.GetFileAsync($"api/employees/downloaddocument?doc_id={doc_id}");
            if (bytes == null) return BadRequest(error ?? "File not found");
            return File(bytes, contentType ?? "application/octet-stream", fileName ?? $"document_{doc_id}");
        }

        // ── Payslip PDF proxy ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PayslipPdf(long payslip_id)
        {
            if (payslip_id <= 0) return BadRequest("payslip_id required");
            var (bytes, contentType, fileName, error) = await _api.GetFileAsync($"api/payroll/payslippdf?payslip_id={payslip_id}");
            if (bytes == null) return BadRequest(error ?? "Failed to generate PDF");
            return File(bytes, contentType ?? "application/pdf", fileName ?? $"payslip_{payslip_id}.pdf");
        }

        // ── Bank details ──────────────────────────────────────────────────────
        public async Task<IActionResult> BankDetails()
        {
            await _audit.LogViewAsync("SelfService/BankDetails");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBanks()
        {
            var r = await _api.GetAsync<object>("api/settings/getbanks");
            return Json(r.IsSuccess ? r.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBankDetails([FromBody] UpdateBankRequest model)
        {
            if (model == null) return Json(new { success = false, message = "Request body is required" });
            var r = await _api.PostAsync<object>("api/me/updatebankdetails", new
            {
                bank_id             = model.bank_id,
                bank_account_no     = model.bank_account_no,
                bank_branch         = model.bank_branch,
                payment_method      = model.payment_method ?? "BANK",
                mobile_money_number = model.mobile_money_number
            });
            return Json(r.IsSuccess
                ? new { success = true, message = "Bank details saved successfully" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed to save bank details" : r.Error });
        }

        // ── HOD approvals ─────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetHodPending()
        {
            var r = await _api.GetAsync<object>("api/me/hodpending");
            if (!r.IsSuccess) return Json(new List<object>());
            return Json(r.Data);
        }

        [HttpPost]
        public async Task<IActionResult> HodReview([FromBody] HodReviewRequest model)
        {
            if (model == null || model.leave_id <= 0)
                return Json(new { success = false, message = "leave_id is required" });
            var r = await _api.PostAsync<object>("api/me/hodreview", new
            {
                leave_id = model.leave_id,
                action   = model.action
            });
            return Json(r.IsSuccess
                ? new { success = true, message = "Done" }
                //? new { success = true,  message = r.Message ?? "Done" }
                : new { success = false, message = string.IsNullOrEmpty(r.Error) ? "Failed" : r.Error });
        }

        // ── Profile photo upload ──────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return Json(new { success = false, message = "No file selected" });

            var ext = Path.GetExtension(photo.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                return Json(new { success = false, message = "Only JPG and PNG files are allowed" });

            if (photo.Length > 3 * 1024 * 1024)
                return Json(new { success = false, message = "File size must be under 3MB" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            var fileName = $"profile_{userId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var savePath = Path.Combine(_env.WebRootPath, "assets", "static", "img", "profile-pics");
            Directory.CreateDirectory(savePath);

            using (var stream = new FileStream(Path.Combine(savePath, fileName), FileMode.Create))
                await photo.CopyToAsync(stream);

            var r = await _api.PostAsync<object>("api/me/updateavatar", new { avatar = fileName });
            if (!r.IsSuccess)
            {
                var bad = Path.Combine(savePath, fileName);
                if (System.IO.File.Exists(bad)) System.IO.File.Delete(bad);
                return Json(new { success = false, message = "Failed to save photo" });
            }

            // Refresh auth cookie so navbar shows new photo immediately
            var claims = User.Claims.Where(c => c.Type != "avatar").ToList();
            claims.Add(new Claim("avatar", fileName));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

            return Json(new { success = true, message = "Profile photo updated", fileName });
        }

        // ── Request models ────────────────────────────────────────────────────
        public class HodReviewRequest
        {
            public long   leave_id { get; set; }
            public string action   { get; set; } = "";
        }
        public class ApplyLeaveRequest
        {
            public long    leave_type_id   { get; set; }
            public string? start_date      { get; set; }
            public string? end_date        { get; set; }
            public string? reason          { get; set; }
            public bool    is_half_day     { get; set; } = false;
            public string? half_day_period { get; set; }
        }
        public class RecallRequest { public long request_id { get; set; } }
        public class AdvanceRequest
        {
            public decimal amount  { get; set; }
            public string? reason  { get; set; }
        }
        public class UpdateProfileRequest
        {
            public string? mobile { get; set; }
            public string? email  { get; set; }
        }
        public class UpdateBankRequest
        {
            public long    bank_id              { get; set; }
            public string? bank_account_no      { get; set; }
            public string? bank_branch          { get; set; }
            public string? payment_method       { get; set; }
            public string? mobile_money_number  { get; set; }
        }
        public class OvertimeRequest
        {
            public string   overtime_date { get; set; } = "";
            public string   start_time    { get; set; } = "";
            public string   end_time      { get; set; } = "";
            public decimal  hourly_rate   { get; set; }
            public string?  reason        { get; set; }
        }

        // ── VIEW: My Overtime ─────────────────────────────────────────────────
        public async Task<IActionResult> MyOvertime()
        {
            await _audit.LogViewAsync("SelfService/MyOvertime");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOvertime()
        {
            var result = await _api.GetAsync<object>("api/me/overtime/myovertime");
            return Json(result.IsSuccess ? result.Data : new List<object>());
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOvertime([FromBody] OvertimeRequest model)
        {
            var result = await _api.PostAsync<object>("api/me/overtime/submit", model);
            return Json(result.IsSuccess
                ? new { success = true, message = "Overtime submitted successfully" }
                : new { success = false, message = string.IsNullOrEmpty(result.Error) ? "Failed to submit" : result.Error });
        }
    }
}
