using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Control_De_Tareas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditController : Controller
    {
        private readonly ContextDB _context;
        private readonly AuditService _auditService;

        public AuditController(ContextDB context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: Audit
        public async Task<IActionResult> Index()
        {
            try
            {
                // Obtener estadísticas del servicio (más eficiente)
                var statistics = await _auditService.GetStatisticsAsync();

                // Pasar estadísticas a la vista
                ViewBag.TotalEventos = statistics.TotalEvents;
                ViewBag.Exitosos = statistics.SuccessfulEvents;
                ViewBag.Fallidos = statistics.FailedEvents;
                ViewBag.EventosHoy = statistics.EventsToday;
                ViewBag.UsuarioMasActivo = statistics.MostActiveUser;
                ViewBag.AccionMasComun = statistics.MostCommonAction;

                // Registrar acceso a auditoría usando el servicio
                await _auditService.LogDashboardAccessAsync("Auditoría", User.Identity?.Name ?? "Administrador");

                // Datos para la tabla
                var auditLogs = await _context.AuditLogs
                    .Include(a => a.User)
                        .ThenInclude(u => u.Rol)
                    .Where(a => !a.IsSoftDeleted)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50)
                    .Select(a => new AuditLogVm
                    {
                        Id = a.Id,
                        UserName = a.User != null ? a.User.UserName : "Usuario Desconocido", 
                        UserRole = a.User != null && a.User.Rol != null ? a.User.Rol.RoleName : "Desconocido",
                        Action = a.Action,
                        Entity = a.Entity,
                        EntityId = a.EntityId,
                        Details = a.Details,
                        CreatedAt = a.CreatedAt,
                        IsSoftDeleted = a.IsSoftDeleted
                    })
                    .ToListAsync();

                return View(auditLogs);
            }
            catch (Exception ex)
            {
                // Registrar error usando el servicio
                await _auditService.LogAsync("ERROR", "Auditoría", null,
                    $"Error al cargar auditoría: {ex.Message}");

                TempData["Error"] = "Error al cargar los registros de auditoría";
                return View(new List<AuditLogVm>());
            }
        }

        // GET: Audit/Detalles/5
        public async Task<IActionResult> Detalles(long id)
        {
            try
            {
                var auditLog = await _context.AuditLogs
                    .Include(a => a.User)
                        .ThenInclude(u => u.Rol)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (auditLog == null || auditLog.IsSoftDeleted)
                {
                    await _auditService.LogAccessDeniedAsync($"Audit/Detalles/{id}", "Registro no encontrado o eliminado");
                    return NotFound();
                }

                var vm = new AuditLogVm
                {
                    Id = auditLog.Id,
                    UserName = auditLog.User != null ? auditLog.User.UserName : "Usuario Desconocido",
                    UserRole = auditLog.User != null && auditLog.User.Rol != null ? auditLog.User.Rol.RoleName : "Desconocido",
                    Action = auditLog.Action,
                    Entity = auditLog.Entity,
                    EntityId = auditLog.EntityId,
                    Details = auditLog.Details,
                    CreatedAt = auditLog.CreatedAt,
                    IsSoftDeleted = auditLog.IsSoftDeleted
                };

                // CORREGIDO: Usar null en lugar de auditLog.Id
                await _auditService.LogAsync("VIEW_DETAILS", "Auditoría", null,
                    $"Vista de detalles del registro de auditoría ID: {auditLog.Id}");

                return View(vm);
            }   
            catch (Exception ex)
            {
                await _auditService.LogAsync("ERROR", "Auditoría", null,
                    $"Error al cargar detalles {id}: {ex.Message}");

                TempData["Error"] = "Error al cargar los detalles del registro";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Audit/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(long id)
        {
            try
            {
                var auditLog = await _context.AuditLogs.FindAsync(id);

                if (auditLog == null)
                {
                    await _auditService.LogAccessDeniedAsync($"Audit/Eliminar/{id}", "Registro no encontrado");
                    return NotFound();
                }

                if (!auditLog.IsSoftDeleted)
                {
                    // Guardar información antes de eliminar
                    var userName = auditLog.User?.UserName ?? "Usuario Desconocido";
                    var action = auditLog.Action;
                    var entity = auditLog.Entity;

                    // Marcar como eliminado
                    auditLog.IsSoftDeleted = true;
                    await _context.SaveChangesAsync();

                    // Registrar eliminación usando el método específico
                    await _auditService.LogAuditRecordDeletionAsync(auditLog.Id,
                        $"(Acción original: {action}, Entidad: {entity}, Usuario: {userName})");

                    TempData["Success"] = "Registro de auditoría archivado correctamente.";
                }
                else
                {
                    TempData["Warning"] = "El registro ya había sido archivado anteriormente.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await _auditService.LogAsync("ERROR", "Auditoría", null,
                    $"Error al eliminar registro {id}: {ex.Message}");

                TempData["Error"] = "Error al archivar el registro";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Audit/Exportar
        public async Task<IActionResult> Exportar(string formato = "csv")
        {
            try
            {
                // Cambia .ToListAsync() por .ToListAsync<dynamic>()
                var auditLogs = await _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => !a.IsSoftDeleted)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => (dynamic)new // Añade (dynamic) aquí
                    {
                        a.Id,
                        Usuario = a.User != null ? a.User.UserName : "Usuario Desconocido",
                        Rol = a.User != null && a.User.Rol != null ? a.User.Rol.RoleName : "Desconocido",
                        a.Action,
                        a.Entity,
                        ID_Entidad = a.EntityId,
                        a.Details,
                        Fecha = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        Estado = a.IsSoftDeleted ? "Eliminado" : "Activo"
                    })
                    .ToListAsync<dynamic>(); // Añade <dynamic> aquí

                // Registrar exportación
                await _auditService.LogAsync("EXPORT", "Auditoría", null,
                    $"Exportación de {auditLogs.Count} registros en formato {formato}");
                    
                if (formato.ToLower() == "json")
                {
                    return ExportToJson(auditLogs);
                }

                return ExportToCsv(auditLogs);
            }
            catch (Exception ex)
            {
                await _auditService.LogAsync("ERROR", "Auditoría", null,
                    $"Error al exportar auditoría: {ex.Message}");

                TempData["Error"] = "Error al exportar los registros";
                return RedirectToAction(nameof(Index));
            }
        }   

        private IActionResult ExportToCsv(List<dynamic> auditLogs)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Usuario,Rol,Acción,Entidad,ID_Entidad,Detalles,Fecha,Estado");

            foreach (var log in auditLogs)
            {
                var detalles = log.Details?.Replace("\"", "\"\"") ?? "";
                csv.AppendLine($"{log.Id},\"{log.Usuario}\",{log.Rol},{log.Action},{log.Entity},{log.ID_Entidad},\"{detalles}\",{log.Fecha},{log.Estado}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private IActionResult ExportToJson(List<dynamic> auditLogs)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(auditLogs, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }
    }
}