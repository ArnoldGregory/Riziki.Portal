// ============================================================
//  RIZIKI.Portal — AuditService
//  Place in: Services/AuditService.cs
//  Lightweight audit logging via NLog (portal-side).
//  Matches the call signatures used across controllers:
//    LogLoginAsync, LogOtpAsync, LogLogoutAsync,
//    LogViewAsync, LogCreateAsync, LogEditAsync, LogDeleteAsync
// ============================================================

using System.Security.Claims;
using System.Text.Json;
using NLog;

namespace RIZIKI.Portal.Services
{
    public sealed class AuditService
    {
        private static readonly Logger _log = LogManager.GetLogger("RIZIKI.Portal.Audit");
        private readonly IHttpContextAccessor _ctx;

        public AuditService(IHttpContextAccessor ctx) { _ctx = ctx; }

        private string User =>
            _ctx.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
            ?? _ctx.HttpContext?.User?.Identity?.Name
            ?? "anonymous";

        private string Ip =>
            _ctx.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        // ── Auth events ──
        public Task LogLoginAsync(string username, bool success, string? detail = null)
        {
            _log.Info($"LOGIN | user={username} | success={success} | ip={Ip} | {detail}");
            return Task.CompletedTask;
        }

        public Task LogOtpAsync(string username, bool success, string? detail = null)
        {
            _log.Info($"OTP | user={username} | success={success} | ip={Ip} | {detail}");
            return Task.CompletedTask;
        }

        public Task LogLogoutAsync()
        {
            _log.Info($"LOGOUT | user={User} | ip={Ip}");
            return Task.CompletedTask;
        }

        // ── CRUD/view events ──
        public Task LogViewAsync(string entity, string? detail = null)
        {
            _log.Info($"VIEW | {entity} | user={User} | {detail}");
            return Task.CompletedTask;
        }

        public Task LogCreateAsync(string entity, object payload, bool success, string? detail = null)
        {
            _log.Info($"CREATE | {entity} | user={User} | success={success} | {Safe(payload)} | {detail}");
            return Task.CompletedTask;
        }

        public Task LogEditAsync(string entity, object payload, bool success, string? detail = null)
        {
            _log.Info($"EDIT | {entity} | user={User} | success={success} | {Safe(payload)} | {detail}");
            return Task.CompletedTask;
        }

        public Task LogDeleteAsync(string entity, object id, bool success, string? detail = null)
        {
            _log.Info($"DELETE | {entity} | id={id} | user={User} | success={success} | {detail}");
            return Task.CompletedTask;
        }

        private static string Safe(object payload)
        {
            try { return JsonSerializer.Serialize(payload); } catch { return "<unserializable>"; }
        }
    }
}