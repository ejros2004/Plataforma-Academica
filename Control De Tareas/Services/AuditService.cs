using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Control_De_Tareas.Services
{
    public class AuditService
    {
        private readonly ContextDB _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ContextDB context, IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ==================== MÉTODOS PRINCIPALES EXISTENTES ====================
        public async Task LogCreateAsync(string entity, Guid? entityId = null, string entityName = "")
        {
            await LogAsync("CREATE", entity, entityId,
                $"Creación de {entity}: {entityName}");
        }

        public async Task LogUpdateAsync(string entity, Guid? entityId = null, string entityName = "")
        {
            await LogAsync("UPDATE", entity, entityId,
                $"Actualización de {entity}: {entityName}");
        }

        public async Task LogDeleteAsync(string entity, Guid? entityId = null, string entityName = "")
        {
            await LogAsync("DELETE", entity, entityId,
                $"Eliminación de {entity}: {entityName}");
        }

        // ==================== MÉTODOS ESPECIALIZADOS NUEVOS ====================
        public async Task LogLoginAsync(string username, bool success, string details = "")
        {
            var action = success ? "LOGIN" : "LOGIN_FAILED";
            var defaultDetails = success
                ? $"Inicio de sesión exitoso para {username}"
                : $"Intento fallido de inicio de sesión para {username}";

            await LogAsync(action, "Autenticación", null,
                string.IsNullOrEmpty(details) ? defaultDetails : details);
        }

        public async Task LogLogoutAsync(string username)
        {
            await LogAsync("LOGOUT", "Autenticación", null,
                $"Cierre de sesión para {username}");
        }

        public async Task LogCourseCreationAsync(string courseName, Guid courseId)
        {
            await LogAsync("CREATE_COURSE", "Cursos", courseId,
                $"Creó el curso '{courseName}'");
        }

        public async Task LogTaskSubmissionAsync(string taskName, Guid taskId, string studentName)
        {
            await LogAsync("SUBMIT_TASK", "Tareas", taskId,
                $"Entregó la tarea '{taskName}' - Estudiante: {studentName}");
        }

        public async Task LogTaskGradingAsync(string taskName, Guid taskId, string studentName, decimal grade)
        {
            await LogAsync("GRADE_TASK", "Calificaciones", taskId,
                $"Calificó la tarea '{taskName}' de {studentName} con {grade}");
        }

        public async Task LogUserDeletionAsync(string userName, Guid userId)
        {
            await LogAsync("DELETE_USER", "Usuarios", userId,
                $"Eliminó al usuario '{userName}'");
        }

        public async Task LogSystemConfigChangeAsync(string configName)
        {
            await LogAsync("MODIFY_CONFIG", "Sistema", null,
                $"Actualizó la configuración del sistema: {configName}");
        }

        public async Task LogAnnouncementCreationAsync(string announcementTitle)
        {
            await LogAsync("CREATE_ANNOUNCEMENT", "Anuncios", null,
                $"Publicó anuncio: '{announcementTitle}'");
        }

        public async Task LogAccessDeniedAsync(string resource, string reason = "")
        {
            await LogAsync("ACCESS_DENIED", "Seguridad", null,
                $"Acceso denegado a {resource}" +
                (string.IsNullOrEmpty(reason) ? "" : $": {reason}"));
        }

        public async Task LogPasswordChangeAsync(string username)
        {
            await LogAsync("PASSWORD_CHANGE", "Seguridad", null,
                $"Cambio de contraseña para {username}");
        }

        public async Task LogEnrollmentAsync(string studentName, Guid courseId, string courseName)
        {
            await LogAsync("ENROLL_STUDENT", "Matrículas", courseId,
                $"Matriculó a {studentName} en el curso '{courseName}'");
        }

        public async Task LogDashboardAccessAsync(string dashboardType, string userName)
        {
            await LogAsync("VIEW_DASHBOARD", "Dashboard", null,
                $"{userName} accedió al dashboard de {dashboardType}");
        }

        // ==================== MÉTODO GENÉRICO MEJORADO ====================
        public async Task LogAsync(string action, string entity, Guid? entityId = null, string details = "")
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var auditLog = new AuditLogs
                {
                    UserId = userId,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = !string.IsNullOrEmpty(details) ? details : $"{action} de {entity} por {userName}",
                    CreatedAt = DateTime.UtcNow,
                    IsSoftDeleted = false
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Auditoría registrada: {Action} - {Entity} - {Details}",
                    action, entity, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando auditoría: {Action}/{Entity}", action, entity);

                // Intentar registrar el error como auditoría fallida
                try
                {
                    var fallbackLog = new AuditLogs
                    {
                        Action = "AUDIT_ERROR",
                        Entity = "Sistema",
                        Details = $"Error al registrar auditoría [{action}/{entity}]: {ex.Message.Substring(0, Math.Min(ex.Message.Length, 200))}",
                        CreatedAt = DateTime.UtcNow,
                        IsSoftDeleted = false
                    };

                    _context.AuditLogs.Add(fallbackLog);
                    await _context.SaveChangesAsync();
                }
                catch (Exception innerEx)
                {
                    _logger.LogCritical(innerEx, "Error crítico en sistema de auditoría");
                }
            }
        }
        public async Task LogAuditRecordDeletionAsync(long auditLogId, string details = "")
        {
            await LogAsync("DELETE_AUDIT_RECORD", "Auditoría", null,
                $"Eliminación de registro de auditoría ID: {auditLogId}. {details}");
        }

        // Estadisticas 
        public async Task<AuditStatisticsVm> GetStatisticsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => !a.IsSoftDeleted);

                return new AuditStatisticsVm
                {
                    TotalEvents = await query.CountAsync(),
                    SuccessfulEvents = await query.CountAsync(a =>
                        a.Action != "LOGIN_FAILED" &&
                        a.Action != "ERROR" &&
                        a.Action != "ACCESS_DENIED" &&
                        a.Action != "AUDIT_ERROR"),
                    FailedEvents = await query.CountAsync(a =>
                        a.Action == "LOGIN_FAILED" ||
                        a.Action == "ERROR" ||
                        a.Action == "ACCESS_DENIED" ||
                        a.Action == "AUDIT_ERROR"),
                    EventsToday = await query.CountAsync(a => a.CreatedAt.Date == today),

                    // Usuario más activo (con más registros)
                    MostActiveUser = await query
                        .Where(a => a.UserId != null && a.User != null)
                        .GroupBy(a => a.UserId)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.First().User.UserName)
                        .FirstOrDefaultAsync() ?? "Ninguno",

                    // Acción más común
                    MostCommonAction = await query
                        .GroupBy(a => a.Action)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefaultAsync() ?? "Ninguna"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de auditoría");
                return new AuditStatisticsVm(); // Retorna objeto vacío en caso de error
            }
        }

        // ==================== MÉTODOS AUXILIARES PRIVADOS ====================
        private Guid? GetCurrentUserId()
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return userId;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo ID de usuario para auditoría");
                return null;
            }
        }

        private string GetCurrentUserName()
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo nombre de usuario para auditoría");
                return "Sistema";
            }
        }

        // Clase para Dashboards 
        public class AuditStatisticsVm
        {
            public int TotalEvents { get; set; }
            public int SuccessfulEvents { get; set; }
            public int FailedEvents { get; set; }
            public int EventsToday { get; set; }
            public string MostActiveUser { get; set; } = "N/A";
            public string MostCommonAction { get; set; } = "N/A";

            public Dictionary<string, int> EventsByAction { get; set; } = new();
            public Dictionary<string, int> EventsByEntity { get; set; } = new();
        }
    }
}