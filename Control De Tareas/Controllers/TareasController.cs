using Control_De_Tareas.Authorization;
using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Control_De_Tareas.Controllers
{
    [Authorize]
    public class TareasController : Controller
    {
        private readonly ContextDB _context;
        private readonly ILogger<TareasController> _logger;
        private readonly AuditService _auditService;

        public TareasController(ContextDB context, ILogger<TareasController> logger, AuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // ========================= LISTADO GENERAL =========================
        public async Task<IActionResult> Index()
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            IQueryable<Tareas> query = _context.Tareas
                .Where(t => !t.IsSoftDeleted)
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // Si es profesor, solo ver tareas de sus cursos
            if (userInfo.Rol?.Nombre == "Profesor")
            {
                query = query.Where(t => t.CourseOffering.ProfessorId == userInfo.UserId);
            }

            var tareas = await query
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            ViewBag.UserRole = userInfo.Rol?.Nombre;
            ViewBag.UserName = userInfo.Nombre;

            return View(tareas);
        }

        // ========================= CREATE =========================
        [Authorize(Roles = "Profesor,Administrador")]
        public async Task<IActionResult> Crear()
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            await LoadCourseOfferingsAsync(userInfo);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesor,Administrador")]
        public async Task<IActionResult> Crear(TareaCreateVm vm)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await LoadCourseOfferingsAsync(userInfo);
                return View(vm);
            }

            // Verificar que el profesor solo pueda crear tareas en sus cursos
            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = await _context.CourseOfferings
                    .AnyAsync(co => co.Id == vm.CourseOfferingId && co.ProfessorId == userInfo.UserId);

                if (!esProfesorDelCurso)
                {
                    TempData["Error"] = "Solo puedes crear tareas en los cursos que impartes.";
                    await LoadCourseOfferingsAsync(userInfo);
                    return View(vm);
                }
            }

            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(claim, out var userGuid))
            {
                ModelState.AddModelError("", "No se pudo identificar al usuario.");
                await LoadCourseOfferingsAsync(userInfo);
                return View(vm);
            }

            var tarea = new Tareas
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = vm.CourseOfferingId,
                Title = vm.Title,
                Description = vm.Description,
                DueDate = vm.DueDate,
                MaxScore = vm.MaxScore,
                CreatedBy = userGuid,
                IsSoftDeleted = false
            };

            _context.Tareas.Add(tarea);
            await _context.SaveChangesAsync();

            await _auditService.LogCreateAsync("Tarea", tarea.Id, tarea.Title);

            TempData["Success"] = "Tarea creada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // ========================= DETALLE =========================
        [Authorize(Roles = "Administrador,Profesor,Estudiante")]
        public async Task<IActionResult> Detalle(Guid id)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            var tarea = await _context.Tareas
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSoftDeleted);

            if (tarea == null)
                return NotFound();

            // Verificar permisos para profesor
            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = tarea.CourseOffering.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    TempData["Error"] = "No tienes permisos para ver esta tarea. Solo puedes ver tareas de tus cursos.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.TotalEntregas = await _context.Submissions.CountAsync(s => s.TaskId == id);
                ViewBag.EntregasCalificadas = await _context.Submissions.CountAsync(s => s.TaskId == id && s.CurrentGrade.HasValue);
            }

            if (userInfo.Rol?.Nombre == "Estudiante")
            {
                var tieneAcceso = await _context.Enrollments.AnyAsync(e =>
                    e.StudentId == userInfo.UserId &&
                    e.CourseOfferingId == tarea.CourseOfferingId &&
                    e.Status == "Active" &&
                    !e.IsSoftDeleted);

                if (!tieneAcceso)
                    return Forbid();

                var submission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.TaskId == id && s.StudentId == userInfo.UserId);

                ViewBag.Submission = submission;
            }

            return View(tarea);
        }

        // ========================= EDITAR GET =========================
        [HttpGet]
        [Authorize(Roles = "Profesor,Administrador")]
        public async Task<IActionResult> Editar(Guid id)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            var tarea = await _context.Tareas
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSoftDeleted);

            if (tarea == null)
                return NotFound();

            // Verificar permisos
            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = tarea.CourseOffering.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    TempData["Error"] = "No tienes permisos para editar esta tarea.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Crear ViewModel
            var vm = new TareaEditVm
            {
                Id = tarea.Id,
                Title = tarea.Title,
                Description = tarea.Description,
                DueDate = tarea.DueDate,
                MaxScore = tarea.MaxScore,
                CourseOfferingId = tarea.CourseOfferingId,
                CreatedBy = tarea.CreatedBy
            };

            // Pasar información del curso a la vista
            ViewBag.CourseOffering = tarea.CourseOffering;
            ViewBag.UserRole = userInfo.Rol?.Nombre;

            return View(vm);
        }

        // ========================= EDITAR POST (con ViewModel) =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesor,Administrador")]
        public async Task<IActionResult> Editar(Guid id, TareaEditVm vm)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            if (id != vm.Id)
                return NotFound();

            // Recargar datos del curso para la vista si hay error
            var tareaOriginal = await _context.Tareas
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tareaOriginal == null)
                return NotFound();

            ViewBag.CourseOffering = tareaOriginal.CourseOffering;
            ViewBag.UserRole = userInfo.Rol?.Nombre;

            // Verificar permisos
            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = tareaOriginal.CourseOffering.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    TempData["Error"] = "No tienes permisos para editar esta tarea.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var tareaExistente = await _context.Tareas
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsSoftDeleted);

                if (tareaExistente == null)
                    return NotFound();

                // Actualizar solo los campos permitidos
                tareaExistente.Title = vm.Title;
                tareaExistente.Description = vm.Description;
                tareaExistente.DueDate = vm.DueDate;
                tareaExistente.MaxScore = vm.MaxScore;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("TASK_UPDATE", "Tarea", tareaExistente.Id,
                    $"Tarea '{tareaExistente.Title}' actualizada por {userInfo.Nombre}");

                TempData["Success"] = "✅ Tarea actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando tarea {TaskId}", id);
                ModelState.AddModelError("", $"Error al actualizar la tarea: {ex.Message}");
                return View(vm);
            }
        }

        // ========================= HELPERS =========================
        private bool TareaExists(Guid id)
        {
            return _context.Tareas.Any(e => e.Id == id && !e.IsSoftDeleted);
        }

        // ========================= DELETE =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesor,Administrador")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userInfo = GetCurrentUser();
                if (userInfo == null)
                    return RedirectToAction("Login", "Account");

                var tarea = await _context.Tareas
                    .Include(t => t.CourseOffering)
                        .ThenInclude(co => co.Course)
                    .Include(t => t.Submissions)
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsSoftDeleted);

                if (tarea == null)
                {
                    TempData["Error"] = "La tarea no existe o ya fue eliminada.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar permisos
                if (userInfo.Rol?.Nombre == "Profesor")
                {
                    var esProfesorDelCurso = tarea.CourseOffering.ProfessorId == userInfo.UserId;
                    if (!esProfesorDelCurso)
                    {
                        var cursoNombre = tarea.CourseOffering.Course?.Title ?? "curso desconocido";
                        TempData["Error"] = $"No tienes permisos para eliminar esta tarea. Esta tarea pertenece al curso '{cursoNombre}' y tú no eres el profesor de ese curso.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                // Administradores pueden eliminar cualquier tarea sin restricción

                // Soft delete de la tarea
                tarea.IsSoftDeleted = true;

                // Soft delete de las entregas relacionadas (si existen)
                if (tarea.Submissions != null && tarea.Submissions.Any())
                {
                    foreach (var submission in tarea.Submissions)
                    {
                        submission.IsSoftDeleted = true;

                        // También eliminar archivos físicos de las entregas
                        var files = await _context.SubmissionFiles
                            .Where(f => f.SubmissionId == submission.Id)
                            .ToListAsync();

                        foreach (var file in files)
                        {
                            // Eliminar archivo físico
                            if (System.IO.File.Exists(file.FilePath))
                            {
                                System.IO.File.Delete(file.FilePath);
                            }
                            file.IsSoftDeleted = true;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("TASK_DELETE", "Tarea", tarea.Id,
                    $"Tarea '{tarea.Title}' eliminada por {userInfo.Nombre}");

                TempData["Success"] = "✅ Tarea eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando tarea {TaskId}", id);
                TempData["Error"] = $"Error al eliminar la tarea: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========================= TAREAS PARA ESTUDIANTES =========================
        [Authorize(Policy = "Estudiante")]
        public async Task<IActionResult> TareasEstudiantes(Guid? courseOfferingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var studentId))
                return RedirectToAction("Login", "Home");

            var enrolledCourses = await _context.Enrollments
                .Where(e => e.StudentId == studentId && !e.IsSoftDeleted)
                .Select(e => e.CourseOfferingId)
                .ToListAsync();

            var tareasQuery = _context.Tareas
                .Where(t => enrolledCourses.Contains(t.CourseOfferingId) && !t.IsSoftDeleted)
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .AsQueryable();

            // FILTRO POR CURSO CUANDO SE DA CLICK EN "VER"
            if (courseOfferingId.HasValue)
            {
                tareasQuery = tareasQuery.Where(t => t.CourseOfferingId == courseOfferingId.Value);
            }

            var tareasConSubmissions = await tareasQuery
                .Select(t => new
                {
                    Tarea = t,
                    Submission = _context.Submissions
                        .FirstOrDefault(s => s.TaskId == t.Id && s.StudentId == studentId)
                })
                .ToListAsync();

            await _auditService.LogAsync("VIEW_TAREAS_ESTUDIANTE", "Tarea", null,
                $"Estudiante accedió a tareas ({tareasConSubmissions.Count})");

            return View(tareasConSubmissions);
        }

        // ========================= HELPERS =========================
        private async Task LoadCourseOfferingsAsync(UserVm userInfo)
        {
            IQueryable<CourseOfferings> query = _context.CourseOfferings
                .Where(co => !co.IsSoftDeleted && co.IsActive)
                .Include(co => co.Course);

            // Si es profesor, solo mostrar los cursos que imparte
            if (userInfo.Rol?.Nombre == "Profesor")
            {
                query = query.Where(co => co.ProfessorId == userInfo.UserId);
            }

            var courseOfferings = await query
                .Select(co => new SelectListItem
                {
                    Value = co.Id.ToString(),
                    Text = $"{co.Course.Code} - {co.Course.Title} - Sección {co.Section}"
                })
                .ToListAsync();

            // Convertir a SelectList
            ViewBag.CourseOfferings = new SelectList(courseOfferings, "Value", "Text");
        }

        private UserVm GetCurrentUser()
        {
            var sesionBase64 = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(sesionBase64)) return null;

            var base64EncodedBytes = Convert.FromBase64String(sesionBase64);
            var sesion = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            return JsonConvert.DeserializeObject<UserVm>(sesion);
        }
    }
}