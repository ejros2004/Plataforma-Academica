using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Control_De_Tareas.Controllers
{
    public class PeriodsController : Controller
    {
        private readonly ContextDB _context;
        private readonly AuditService _auditService;

        public PeriodsController(ContextDB context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: Periods
        public async Task<IActionResult> Index()
        {
            // Verificar si es administrador
            if (!IsAdmin())
            {
                TempData["Error"] = "No tiene permisos para acceder a esta página";
                return RedirectToAction("Index", "Home");
            }

            var periods = await _context.Periods
                .Where(p => !p.IsSoftDeleted)
                .Include(p => p.CourseOfferings)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            var models = periods.Select(p => new PeriodVm
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CourseOfferingsCount = p.CourseOfferings.Count(co => !co.IsSoftDeleted)
            }).ToList();

            return View(models);
        }

        // GET: Periods/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "No tiene permisos para acceder a esta página";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var period = await _context.Periods
                .Include(p => p.CourseOfferings)
                    .ThenInclude(co => co.Course)
                .Include(p => p.CourseOfferings)
                    .ThenInclude(co => co.Professor)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsSoftDeleted);

            if (period == null)
            {
                return NotFound();
            }

            var model = new PeriodVm
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                IsActive = period.IsActive,
                CreatedAt = period.CreatedAt,
                CourseOfferingsCount = period.CourseOfferings.Count(co => !co.IsSoftDeleted)
            };

            return View(model);
        }

        // GET: Periods/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden crear períodos";
                return RedirectToAction("Index");
            }

            var model = new PeriodVm
            {
                StartDate = DateTime.Now.Date,
                EndDate = DateTime.Now.AddMonths(6).Date,
                IsActive = false
            };
            return View(model);
        }

        // POST: Periods/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PeriodVm model)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden crear períodos";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var hasOverlap = await _context.Periods
                .AnyAsync(p => !p.IsSoftDeleted &&
                    ((model.StartDate >= p.StartDate && model.StartDate <= p.EndDate) ||
                     (model.EndDate >= p.StartDate && model.EndDate <= p.EndDate) ||
                     (model.StartDate <= p.StartDate && model.EndDate >= p.EndDate)));

            if (hasOverlap)
            {
                ModelState.AddModelError("",
                    "Las fechas de este período se solapan con otro período existente.");
                return View(model);
            }

            // Si se está creando un período activo, desactivar todos los demás
            if (model.IsActive)
            {
                var activePeriods = await _context.Periods
                    .Where(p => p.IsActive && !p.IsSoftDeleted)
                    .ToListAsync();

                foreach (var ap in activePeriods)
                {
                    ap.IsActive = false;
                }
            }

            var period = new Periods
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                IsSoftDeleted = false
            };

            _context.Periods.Add(period);
            await _context.SaveChangesAsync();

            // AUDITORÍA: Período creado
            await _auditService.LogCreateAsync("Período", period.Id,
                $"{model.Name} ({model.StartDate:dd/MM/yyyy} - {model.EndDate:dd/MM/yyyy})");

            TempData["Success"] = "Período creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Periods/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden editar períodos";
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            var period = await _context.Periods
                .Include(p => p.CourseOfferings)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsSoftDeleted);

            if (period == null)
            {
                return NotFound();
            }

            var model = new PeriodVm
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                IsActive = period.IsActive,
                CreatedAt = period.CreatedAt,
                CourseOfferingsCount = period.CourseOfferings.Count(co => !co.IsSoftDeleted)
            };

            return View(model);
        }

        // POST: Periods/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PeriodVm model)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden editar períodos";
                return RedirectToAction("Index");
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var period = await _context.Periods
                .Include(p => p.CourseOfferings)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsSoftDeleted);

            if (period == null)
            {
                return NotFound();
            }

            if (period.CourseOfferings.Any(co => !co.IsSoftDeleted))
            {
                ModelState.AddModelError("",
                    "No se puede editar un período que ya tiene ofertas de cursos asignadas.");
                return View(model);
            }

            // Verificar solapamiento con otros períodos (excepto este)
            var hasOverlap = await _context.Periods
                .AnyAsync(p => !p.IsSoftDeleted && p.Id != id &&
                    ((model.StartDate >= p.StartDate && model.StartDate <= p.EndDate) ||
                     (model.EndDate >= p.StartDate && model.EndDate <= p.EndDate) ||
                     (model.StartDate <= p.StartDate && model.EndDate >= p.EndDate)));

            if (hasOverlap)
            {
                ModelState.AddModelError("",
                    "Las fechas de este período se solapan con otro período existente.");
                return View(model);
            }

            // Si se está activando este período, desactivar todos los demás
            if (model.IsActive && !period.IsActive)
            {
                var activePeriods = await _context.Periods
                    .Where(p => p.IsActive && !p.IsSoftDeleted && p.Id != id)
                    .ToListAsync();

                foreach (var ap in activePeriods)
                {
                    ap.IsActive = false;
                }
            }

            period.Name = model.Name;
            period.StartDate = model.StartDate;
            period.EndDate = model.EndDate;
            period.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            // AUDITORÍA: Período actualizado
            await _auditService.LogUpdateAsync("Período", period.Id,
                $"{model.Name} ({model.StartDate:dd/MM/yyyy} - {model.EndDate:dd/MM/yyyy})");

            TempData["Success"] = "Período actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Periods/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden eliminar períodos";
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            var period = await _context.Periods
                .Include(p => p.CourseOfferings)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsSoftDeleted);

            if (period == null)
            {
                return NotFound();
            }

            var model = new PeriodVm
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                IsActive = period.IsActive,
                CreatedAt = period.CreatedAt,
                CourseOfferingsCount = period.CourseOfferings.Count(co => !co.IsSoftDeleted)
            };

            return View(model);
        }

        // POST: Periods/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden eliminar períodos";
                return RedirectToAction(nameof(Index));
            }

            // Verificar ID válido
            if (id == Guid.Empty)
            {
                TempData["Error"] = "ID del período no válido";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Usar transaction para garantizar consistencia
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 1. Verificar si hay ofertas de cursos activas
                var hasActiveOfferings = await _context.CourseOfferings
                    .AnyAsync(co => co.PeriodId == id && !co.IsSoftDeleted);

                if (hasActiveOfferings)
                {
                    // 2. Primero, hacer soft delete de todas las ofertas de cursos
                    var activeOfferings = await _context.CourseOfferings
                        .Where(co => co.PeriodId == id && !co.IsSoftDeleted)
                        .ToListAsync();

                    foreach (var offering in activeOfferings)
                    {
                        offering.IsSoftDeleted = true;
                        offering.IsActive = false;
                    }

                    await _context.SaveChangesAsync();

                    // 3. Verificar si las ofertas tenían dependencias
                    var offeringIds = activeOfferings.Select(o => o.Id).ToList();

                    var hasEnrollments = await _context.Enrollments
                        .AnyAsync(e => offeringIds.Contains(e.CourseOfferingId) && !e.IsSoftDeleted);

                    var hasTareas = await _context.Tareas
                        .AnyAsync(t => offeringIds.Contains(t.CourseOfferingId) && !t.IsSoftDeleted);

                    var hasAnnouncements = await _context.Announcements
                        .AnyAsync(a => offeringIds.Contains(a.CourseOfferingId) && !a.IsSoftDeleted);

                    if (hasEnrollments || hasTareas || hasAnnouncements)
                    {
                        await transaction.RollbackAsync();

                        // AUDITORÍA: Error al eliminar período por dependencias
                        await _auditService.LogAsync("PERIOD_DELETE_DEPENDENCY_ERROR", "Período", id,
                            $"No se puede eliminar el período {id} porque tiene ofertas con inscripciones, tareas o anuncios activos");

                        TempData["Error"] = "No se puede eliminar el período porque tiene ofertas con inscripciones, tareas o anuncios activos. Primero elimine estas dependencias.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // 4. Finalmente, hacer soft delete del período
                var period = await _context.Periods
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsSoftDeleted);

                if (period == null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Período no encontrado o ya eliminado";
                    return RedirectToAction(nameof(Index));
                }

                period.IsSoftDeleted = true;
                period.IsActive = false;

                await _context.SaveChangesAsync();

                // AUDITORÍA: Período eliminado
                await _auditService.LogDeleteAsync("Período", period.Id,
                    $"{period.Name} ({period.StartDate:dd/MM/yyyy} - {period.EndDate:dd/MM/yyyy})");

                await transaction.CommitAsync();

                TempData["Success"] = "Período eliminado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // AUDITORÍA: Error de base de datos
                await _auditService.LogAsync("PERIOD_DELETE_DB_ERROR", "Período", id,
                    $"Error de base de datos al eliminar período: {dbEx.Message}");

                TempData["Error"] = "Error de base de datos. Asegúrese de haber actualizado la configuración de eliminación en el ContextDB.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // AUDITORÍA: Error inesperado
                await _auditService.LogAsync("PERIOD_DELETE_ERROR", "Período", id,
                    $"Error inesperado al eliminar período: {ex.Message}");

                TempData["Error"] = $"Error inesperado: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Periods/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden cambiar el estado de períodos";
                return RedirectToAction("Index");
            }

            var period = await _context.Periods
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsSoftDeleted);

            if (period == null)
            {
                return NotFound();
            }

            var oldStatus = period.IsActive;

            if (period.IsActive)
            {
                // Si está activo, simplemente desactivarlo
                period.IsActive = false;
                TempData["Success"] = "Período desactivado exitosamente";
            }
            else
            {
                // Si está inactivo, desactivar todos los demás y activar este
                var activePeriods = await _context.Periods
                    .Where(p => p.IsActive && !p.IsSoftDeleted && p.Id != id)
                    .ToListAsync();

                foreach (var ap in activePeriods)
                {
                    ap.IsActive = false;
                }

                period.IsActive = true;
                TempData["Success"] = "Período activado exitosamente (otros períodos fueron desactivados)";
            }

            await _context.SaveChangesAsync();

            // AUDITORÍA: Estado de período cambiado
            await _auditService.LogAsync("PERIOD_TOGGLE_ACTIVE", "Período", period.Id,
                $"Período {period.Name} cambió de {(oldStatus ? "Activo" : "Inactivo")} a {(period.IsActive ? "Activo" : "Inactivo")}");

            return RedirectToAction(nameof(Index));
        }

        #region Helper Methods

        private string GetCurrentUserRole()
        {
            var sesionBase64 = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(sesionBase64))
                return "Estudiante";

            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(sesionBase64);
                var sesion = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                var userInfo = System.Text.Json.JsonSerializer.Deserialize<UserVm>(sesion);
                return userInfo?.Rol?.Nombre ?? "Estudiante";
            }
            catch
            {
                return "Estudiante";
            }
        }

        private bool IsAdmin()
        {
            return GetCurrentUserRole() == "Administrador";
        }

        #endregion
    }
}