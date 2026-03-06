using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;

namespace Control_De_Tareas.Controllers
{
    public class CourseOfferingsController : Controller
    {
        private readonly ContextDB _context;
        private readonly ILogger<CourseOfferingsController> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly AuditService _auditService;
        private readonly IAntiforgery _antiforgery;

        public CourseOfferingsController(
            ContextDB context,
            ILogger<CourseOfferingsController> logger,
            IFileStorageService fileStorageService,
            AuditService auditService,
            IAntiforgery antiforgery)
        {
            _context = context;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _auditService = auditService;
            _antiforgery = antiforgery;
        }

        // ========== MÉTODOS PRINCIPALES ==========

        // GET: CourseOfferings/MisCursos
        public async Task<IActionResult> MisCursos()
        {
            var userSession = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSession))
            {
                return RedirectToAction("Login", "Account");
            }

            var userJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(userSession));
            var user = JsonSerializer.Deserialize<UserVm>(userJson);

            // Si es administrador, mostrar todas las ofertas
            if (IsAdminFromSession())
            {
                return RedirectToAction("ListadoOfertas");
            }

            var query = _context.CourseOfferings
                .Where(co => !co.IsSoftDeleted && co.IsActive)
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                .Include(co => co.Tareas)
                .AsQueryable();

            // En el método MisCursos(), en la parte donde filtra según rol:
            string userRole = "Estudiante";
            var currentUserId = GetCurrentUserId();

            if (IsProfesorFromSession())
            {
                userRole = "Profesor";
                query = query.Where(co => co.ProfessorId == currentUserId && co.IsActive); // Agregar && co.IsActive
            }
            else if (IsEstudianteFromSession())
            {
                userRole = "Estudiante";
                query = query.Where(co => co.IsActive && // Agregar co.IsActive
                    co.Enrollments.Any(e => e.StudentId == currentUserId &&
                        !e.IsSoftDeleted &&
                            e.Status == "Active"));
            }

            var offerings = await query
                .OrderByDescending(co => co.Period.StartDate)
                .ThenBy(co => co.Course.Title)
                .ToListAsync();

            var models = offerings.Select(co => new CourseOfferingVm
            {
                Id = co.Id,
                CourseId = co.CourseId,
                CourseName = co.Course?.Title ?? "Sin nombre",
                CourseCode = co.Course?.Code ?? "",
                ProfessorName = co.Professor?.UserName ?? "Sin asignar",
                PeriodName = co.Period?.Name ?? "Sin período",
                Section = co.Section,
                PeriodStartDate = co.Period?.StartDate,
                PeriodEndDate = co.Period?.EndDate,
                EnrolledStudentsCount = co.Enrollments?.Count(e => !e.IsSoftDeleted) ?? 0,
                TasksCount = co.Tareas?.Count(t => !t.IsSoftDeleted) ?? 0,
                IsActive = co.IsActive,
                Status = CalculateStatus(co.Period?.StartDate, co.Period?.EndDate, co.IsActive)
            }).ToList();

            ViewBag.UserRole = userRole;
            return View(models);
        }

        // GET: CourseOfferings/CursosDisponibles
        public async Task<IActionResult> CursosDisponibles()
        {
            // Verificar que sea estudiante
            if (!IsEstudianteFromSession())
            {
                TempData["Error"] = "Solo los estudiantes pueden ver cursos disponibles";
                return RedirectToAction("MisCursos");
            }

            var currentUserId = GetCurrentUserId();

            // 1. Obtener IDs de cursos donde ya está inscrito
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == currentUserId && !e.IsSoftDeleted)
                .Select(e => e.CourseOfferingId)
                .ToListAsync();

            // 2. Obtener ofertas disponibles (activas, período vigente, no inscrito)
            var offerings = await _context.CourseOfferings
                .Where(co => !co.IsSoftDeleted &&
                           co.IsActive &&
                           !enrolledCourseIds.Contains(co.Id) &&
                           co.Period.EndDate >= DateTime.Now.Date && // Período aún no termina
                           co.Period.StartDate <= DateTime.Now.Date) // Período ya inició o está en curso
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                .OrderByDescending(co => co.Period.StartDate) // Más recientes primero
                .ThenBy(co => co.Course.Title)
                .ThenBy(co => co.Section)
                .ToListAsync();

            // 3. Mapear a ViewModel
            var models = offerings.Select(co => new CourseOfferingVm
            {
                Id = co.Id,
                CourseId = co.CourseId,
                CourseName = co.Course?.Title ?? "Sin nombre",
                CourseCode = co.Course?.Code ?? "",
                ProfessorName = co.Professor?.UserName ?? "Sin asignar",
                ProfessorEmail = co.Professor?.Email ?? "",
                PeriodName = co.Period?.Name ?? "Sin período",
                Section = co.Section,
                PeriodStartDate = co.Period?.StartDate,
                PeriodEndDate = co.Period?.EndDate,
                EnrolledStudentsCount = co.Enrollments?.Count(e => !e.IsSoftDeleted) ?? 0,
                TasksCount = co.Tareas?.Count(t => !t.IsSoftDeleted) ?? 0,
                IsActive = co.IsActive,
                CreatedAt = co.CreatedAt,
                Status = CalculateStatus(co.Period?.StartDate, co.Period?.EndDate, co.IsActive)
            }).ToList();

            ViewBag.UserRole = "Estudiante";

            // Agregar token anti-forgery para la vista
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.RequestToken = tokens.RequestToken;

            return View(models);
        }

        // GET: CourseOfferings/ListadoOfertas
        public async Task<IActionResult> ListadoOfertas(Guid? periodId)
        {
            // Verificar si es administrador
            if (!IsAdmin())
            {
                TempData["Error"] = "No tiene permisos para acceder a esta página";
                return RedirectToAction("MisCursos");
            }

            var query = _context.CourseOfferings
                .Where(co => !co.IsSoftDeleted)
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                .Include(co => co.Tareas)
                .Include(co => co.Announcements)
                .AsQueryable();

            if (periodId.HasValue)
            {
                query = query.Where(co => co.PeriodId == periodId.Value);
            }

            var offerings = await query
                .OrderByDescending(co => co.Period.StartDate)
                .ThenBy(co => co.Course != null ? co.Course.Title : "")
                .ToListAsync();

            var models = offerings.Select(co => new CourseOfferingVm
            {
                Id = co.Id,
                CourseId = co.CourseId,
                ProfessorId = co.ProfessorId,
                PeriodId = co.PeriodId,
                Section = co.Section,
                CreatedAt = co.CreatedAt,
                IsActive = co.IsActive,
                CourseName = co.Course?.Title ?? "Sin nombre",
                CourseCode = co.Course?.Code ?? "",
                ProfessorName = co.Professor?.UserName ?? "Sin asignar",
                ProfessorEmail = co.Professor?.Email ?? "",
                PeriodName = co.Period?.Name ?? "Sin período",
                PeriodStartDate = co.Period?.StartDate,
                PeriodEndDate = co.Period?.EndDate,
                EnrolledStudentsCount = co.Enrollments?.Count(e => !e.IsSoftDeleted) ?? 0,
                TasksCount = co.Tareas?.Count(t => !t.IsSoftDeleted) ?? 0,
                AnnouncementsCount = co.Announcements?.Count(a => !a.IsSoftDeleted) ?? 0,
                Status = CalculateStatus(co.Period?.StartDate, co.Period?.EndDate, co.IsActive)
            }).ToList();

            ViewBag.Periods = await _context.Periods
                .Where(p => !p.IsSoftDeleted)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = periodId.HasValue && p.Id == periodId.Value
                })
                .ToListAsync();

            ViewBag.SelectedPeriodId = periodId;
            ViewBag.UserRole = GetCurrentUserRole();
            return View(models);
        }

        // GET: CourseOfferings/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var offering = await _context.CourseOfferings
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments).ThenInclude(e => e.Student)
                .Include(co => co.Tareas)
                .Include(co => co.Announcements)
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null) return NotFound();

            // Verificar permisos
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole == "Estudiante")
            {
                // Verificar si el estudiante está inscrito
                var isEnrolled = offering.Enrollments?
                    .Any(e => e.StudentId == userId && !e.IsSoftDeleted) ?? false;

                if (!isEnrolled)
                {
                    TempData["Error"] = "No tienes acceso a este curso";
                    return RedirectToAction("MisCursos");
                }
            }
            else if (userRole == "Profesor" && offering.ProfessorId != userId)
            {
                TempData["Error"] = "No tienes permisos para ver este curso";
                return RedirectToAction("MisCursos");
            }

            var model = new CourseOfferingVm
            {
                Id = offering.Id,
                CourseId = offering.CourseId,
                ProfessorId = offering.ProfessorId,
                PeriodId = offering.PeriodId,
                Section = offering.Section,
                CreatedAt = offering.CreatedAt,
                IsActive = offering.IsActive,
                CourseName = offering.Course?.Title ?? "Sin nombre",
                CourseCode = offering.Course?.Code ?? "",
                ProfessorName = offering.Professor?.UserName ?? "Sin asignar",
                ProfessorEmail = offering.Professor?.Email ?? "",
                PeriodName = offering.Period?.Name ?? "Sin período",
                PeriodStartDate = offering.Period?.StartDate,
                PeriodEndDate = offering.Period?.EndDate,
                EnrolledStudentsCount = offering.Enrollments?.Count(e => !e.IsSoftDeleted) ?? 0,
                TasksCount = offering.Tareas?.Count(t => !t.IsSoftDeleted) ?? 0,
                AnnouncementsCount = offering.Announcements?.Count(a => !a.IsSoftDeleted) ?? 0,
                Status = CalculateStatus(offering.Period?.StartDate, offering.Period?.EndDate, offering.IsActive)
            };

            ViewBag.UserRole = userRole;
            ViewBag.IsProfesor = (userRole == "Profesor" && offering.ProfessorId == userId) || userRole == "Administrador";
            ViewBag.IsAdmin = userRole == "Administrador";
            return View(model);
        }

        // GET: CourseOfferings/Index/5 (Gestión de inscripciones)
        public async Task<IActionResult> Index(Guid? id)
        {
            if (id == null)
            {
                return RedirectToAction("MisCursos");
            }

            // Verificar permisos - Solo admin o profesor del curso
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var offering = await _context.CourseOfferings
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null)
            {
                return NotFound();
            }

            // Verificar si el usuario tiene acceso
            if (userRole != "Administrador" && offering.ProfessorId != userId)
            {
                TempData["Error"] = "No tiene permisos para gestionar este curso";
                return RedirectToAction("MisCursos");
            }

            var model = new CourseOfferingVm
            {
                Id = offering.Id,
                CourseName = offering.Course?.Title ?? "Sin nombre",
                CourseCode = offering.Course?.Code ?? "",
                ProfessorName = offering.Professor?.UserName ?? "Sin asignar",
                PeriodName = offering.Period?.Name ?? "Sin período",
                Section = offering.Section,
                EnrolledStudentsCount = offering.Enrollments?.Count(e => !e.IsSoftDeleted) ?? 0,
                Status = CalculateStatus(offering.Period?.StartDate, offering.Period?.EndDate, offering.IsActive)
            };

            ViewBag.EnrolledStudents = offering.Enrollments
                .Where(e => !e.IsSoftDeleted)
                .Select(e => new EnrollmentVm
                {
                    Id = e.Id,
                    CourseOfferingId = e.CourseOfferingId,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.UserName ?? "Sin nombre",
                    StudentEmail = e.Student?.Email ?? "",
                    EnrolledAt = e.EnrolledAt,
                    Status = e.Status,
                    CourseName = offering.Course?.Title ?? "",
                    CourseCode = offering.Course?.Code ?? "",
                    ProfessorName = offering.Professor?.UserName ?? "",
                    PeriodName = offering.Period?.Name ?? "",
                    Section = offering.Section
                })
                .OrderBy(s => s.StudentName)
                .ToList();

            var enrolledStudentIds = offering.Enrollments
                .Where(e => !e.IsSoftDeleted)
                .Select(e => e.StudentId)
                .ToList();

            ViewBag.AvailableStudents = await _context.Users
                .Where(u => !u.IsSoftDeleted &&
                           u.UserRoles.Any(ur => ur.Role.RoleName == "Estudiante") &&
                           !enrolledStudentIds.Contains(u.UserId))
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = $"{u.UserName} - {u.Email}"
                })
                .ToListAsync();

            ViewBag.UserRole = userRole;
            return View(model);
        }

        // ========== CRUD PARA ADMINISTRADORES ==========

        // GET: CourseOfferings/Create
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden crear ofertas";
                return RedirectToAction("MisCursos");
            }

            await LoadDropdownsAsync();
            ViewBag.UserRole = "Administrador";
            return View(new CourseOfferingVm());
        }

        // POST: CourseOfferings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseOfferingVm model)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden crear ofertas";
                return RedirectToAction("MisCursos");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                ViewBag.UserRole = "Administrador";
                return View(model);
            }

            var exists = await _context.CourseOfferings
                .AnyAsync(co => !co.IsSoftDeleted &&
                    co.CourseId == model.CourseId &&
                    co.PeriodId == model.PeriodId &&
                    co.Section == model.Section);

            if (exists)
            {
                ModelState.AddModelError("", "Ya existe una oferta para este curso, período y sección.");
                await LoadDropdownsAsync();
                ViewBag.UserRole = "Administrador";
                return View(model);
            }

            var offering = new CourseOfferings
            {
                Id = Guid.NewGuid(),
                CourseId = model.CourseId,
                ProfessorId = model.ProfessorId,
                PeriodId = model.PeriodId,
                Section = model.Section,
                CreatedAt = DateTime.Now,
                IsActive = model.IsActive,
                IsSoftDeleted = false
            };

            _context.CourseOfferings.Add(offering);
            await _context.SaveChangesAsync();

            // AUDITORÍA: Oferta de curso creada
            await _auditService.LogCreateAsync("OfertaCurso", offering.Id,
                $"{model.Section} - {await GetCourseName(model.CourseId)}");

            TempData["Success"] = "Oferta de curso creada exitosamente";
            return RedirectToAction("ListadoOfertas");
        }

        // GET: CourseOfferings/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden editar ofertas";
                return RedirectToAction("MisCursos");
            }

            var offering = await _context.CourseOfferings
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null) return NotFound();

            var model = new CourseOfferingVm
            {
                Id = offering.Id,
                CourseId = offering.CourseId,
                ProfessorId = offering.ProfessorId,
                PeriodId = offering.PeriodId,
                Section = offering.Section,
                CreatedAt = offering.CreatedAt,
                IsActive = offering.IsActive
            };

            await LoadDropdownsAsync(model);
            ViewBag.UserRole = "Administrador";
            return View(model);
        }

        // POST: CourseOfferings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CourseOfferingVm model)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden editar ofertas";
                return RedirectToAction("MisCursos");
            }

            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model);
                ViewBag.UserRole = "Administrador";
                return View(model);
            }

            var offering = await _context.CourseOfferings
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null) return NotFound();

            var exists = await _context.CourseOfferings
                .AnyAsync(co => !co.IsSoftDeleted &&
                    co.Id != id &&
                    co.CourseId == model.CourseId &&
                    co.PeriodId == model.PeriodId &&
                    co.Section == model.Section);

            if (exists)
            {
                ModelState.AddModelError("", "Ya existe otra oferta para este curso, período y sección.");
                await LoadDropdownsAsync(model);
                ViewBag.UserRole = "Administrador";
                return View(model);
            }

            // Guardar valores antiguos para auditoría
            var oldValues = new
            {
                CourseId = offering.CourseId,
                ProfessorId = offering.ProfessorId,
                PeriodId = offering.PeriodId,
                Section = offering.Section,
                IsActive = offering.IsActive
            };

            offering.CourseId = model.CourseId;
            offering.ProfessorId = model.ProfessorId;
            offering.PeriodId = model.PeriodId;
            offering.Section = model.Section;
            offering.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            // AUDITORÍA: Oferta de curso actualizada
            await _auditService.LogUpdateAsync("OfertaCurso", offering.Id,
                $"{model.Section} - {await GetCourseName(model.CourseId)}");

            TempData["Success"] = "Oferta actualizada exitosamente";
            return RedirectToAction("ListadoOfertas");
        }

        // GET: CourseOfferings/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden eliminar ofertas";
                return RedirectToAction("MisCursos");
            }

            var offering = await _context.CourseOfferings
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null) return NotFound();

            var model = new CourseOfferingVm
            {
                Id = offering.Id,
                CourseName = offering.Course?.Title ?? "Sin nombre",
                ProfessorName = offering.Professor?.UserName ?? "Sin asignar",
                PeriodName = offering.Period?.Name ?? "Sin período",
                Section = offering.Section,
                EnrolledStudentsCount = offering.Enrollments?.Count(e => !e.IsSoftDeleted) ?? 0
            };

            ViewBag.UserRole = "Administrador";
            return View(model);
        }

        // POST: CourseOfferings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden eliminar ofertas";
                return RedirectToAction("MisCursos");
            }

            var offering = await _context.CourseOfferings
                .Include(co => co.Enrollments)
                .Include(co => co.Course)
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null) return NotFound();

            if (offering.Enrollments.Any(e => !e.IsSoftDeleted))
            {
                // AUDITORÍA: Intento de eliminar oferta con estudiantes inscritos
                await _auditService.LogAsync("OFFERING_DELETE_WITH_ENROLLMENTS", "OfertaCurso", offering.Id,
                    $"Intento de eliminar oferta {offering.Section} - {offering.Course?.Title} con {offering.Enrollments.Count(e => !e.IsSoftDeleted)} estudiantes inscritos");

                TempData["Error"] = "No se puede eliminar una oferta que tiene estudiantes inscritos.";
                return RedirectToAction("ListadoOfertas");
            }

            offering.IsSoftDeleted = true;
            offering.IsActive = false;

            await _context.SaveChangesAsync();

            // AUDITORÍA: Oferta de curso eliminada
            await _auditService.LogDeleteAsync("OfertaCurso", offering.Id,
                $"{offering.Section} - {offering.Course?.Title ?? "Sin nombre"}");

            TempData["Success"] = "Oferta eliminada exitosamente";
            return RedirectToAction("ListadoOfertas");
        }

        // ========== GESTIÓN DE INSCRIPCIONES ==========

        // POST: CourseOfferings/InscribirEstudiante
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InscribirEstudiante(Guid courseOfferingId, Guid studentId)
        {
            try
            {
                // Verificar permisos - Solo admin o profesor del curso
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();

                var offering = await _context.CourseOfferings
                    .Include(co => co.Course)
                    .FirstOrDefaultAsync(co => co.Id == courseOfferingId && !co.IsSoftDeleted);

                if (offering == null)
                {
                    TempData["Error"] = "No se encontró la oferta de curso";
                    return RedirectToAction("MisCursos");
                }

                if (userRole != "Administrador" && offering.ProfessorId != userId)
                {
                    TempData["Error"] = "No tiene permisos para inscribir estudiantes en este curso";
                    return RedirectToAction("MisCursos");
                }

                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseOfferingId == courseOfferingId &&
                                             e.StudentId == studentId &&
                                             !e.IsSoftDeleted);

                if (existingEnrollment != null)
                {
                    TempData["Error"] = "El estudiante ya está inscrito en este curso.";
                    return RedirectToAction("Index", new { id = courseOfferingId });
                }

                var enrollment = new Enrollments
                {
                    Id = Guid.NewGuid(),
                    CourseOfferingId = courseOfferingId,
                    StudentId = studentId,
                    EnrolledAt = DateTime.Now,
                    Status = "Active",
                    IsSoftDeleted = false
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                // AUDITORÍA: Estudiante inscrito
                await _auditService.LogAsync("ENROLL_CREATE", "Matrícula", enrollment.Id,
                    $"Estudiante {await GetStudentName(studentId)} inscrito en {offering.Course?.Title} - Sección {offering.Section}");

                TempData["Success"] = "Estudiante inscrito exitosamente";
                return RedirectToAction("Index", new { id = courseOfferingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inscribiendo estudiante {StudentId} en oferta {CourseOfferingId}", studentId, courseOfferingId);

                // AUDITORÍA: Error al inscribir estudiante
                await _auditService.LogAsync("ENROLL_CREATE_ERROR", "Matrícula", null,
                    $"Error al inscribir estudiante {studentId} en oferta {courseOfferingId}: {ex.Message}");

                TempData["Error"] = "Error al inscribir estudiante.";
                return RedirectToAction("Index", new { id = courseOfferingId });
            }
        }

        // POST: CourseOfferings/DesinscribirEstudiante
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesinscribirEstudiante(Guid courseOfferingId, Guid studentId)
        {
            try
            {
                // Verificar permisos - Solo admin o profesor del curso
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();

                var offering = await _context.CourseOfferings
                    .Include(co => co.Course)
                    .FirstOrDefaultAsync(co => co.Id == courseOfferingId && !co.IsSoftDeleted);

                if (offering == null)
                {
                    TempData["Error"] = "No se encontró la oferta de curso";
                    return RedirectToAction("MisCursos");
                }

                if (userRole != "Administrador" && offering.ProfessorId != userId)
                {
                    TempData["Error"] = "No tiene permisos para desinscribir estudiantes en este curso";
                    return RedirectToAction("MisCursos");
                }

                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseOfferingId == courseOfferingId &&
                                             e.StudentId == studentId &&
                                             !e.IsSoftDeleted);

                if (enrollment == null)
                {
                    TempData["Error"] = "No se encontró la inscripción del estudiante.";
                    return RedirectToAction("Index", new { id = courseOfferingId });
                }

                enrollment.IsSoftDeleted = true;
                enrollment.Status = "Dropped";

                await _context.SaveChangesAsync();

                // AUDITORÍA: Estudiante desinscrito
                await _auditService.LogAsync("ENROLL_DELETE", "Matrícula", enrollment.Id,
                    $"Estudiante {await GetStudentName(studentId)} desinscrito de {offering.Course?.Title} - Sección {offering.Section}");

                TempData["Success"] = "Estudiante desinscrito exitosamente";
                return RedirectToAction("Index", new { id = courseOfferingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desinscribiendo estudiante {StudentId} de oferta {CourseOfferingId}", studentId, courseOfferingId);

                // AUDITORÍA: Error al desinscribir estudiante
                await _auditService.LogAsync("ENROLL_DELETE_ERROR", "Matrícula", null,
                    $"Error al desinscribir estudiante {studentId} de oferta {courseOfferingId}: {ex.Message}");

                TempData["Error"] = "Error al desinscribir estudiante.";
                return RedirectToAction("Index", new { id = courseOfferingId });
            }
        }

        // POST: CourseOfferings/MatricularEstudiante (automatrícula) - VERSIÓN CORREGIDA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MatricularEstudiante(Guid courseOfferingId)
        {
            try
            {
                _logger.LogInformation("Iniciando proceso de matrícula para oferta: {CourseOfferingId}", courseOfferingId);

                var userSession = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSession))
                {
                    _logger.LogWarning("Sesión no válida al intentar matricular estudiante");
                    return Json(new
                    {
                        success = false,
                        message = "Sesión no válida. Por favor, inicia sesión nuevamente."
                    });
                }

                var userJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(userSession));
                var user = JsonSerializer.Deserialize<UserVm>(userJson);

                if (user == null)
                {
                    _logger.LogWarning("Usuario no encontrado en sesión");
                    return Json(new
                    {
                        success = false,
                        message = "Usuario no encontrado. Por favor, inicia sesión nuevamente."
                    });
                }

                _logger.LogInformation("Usuario intentando matricularse: {UserId} - {UserName}", user.UserId, user.Nombre);

                // Verificar que sea estudiante
                if (!IsEstudianteFromSession())
                {
                    _logger.LogWarning("Usuario no es estudiante: {Rol}", user.Rol?.Nombre);
                    return Json(new
                    {
                        success = false,
                        message = "Solo los estudiantes pueden matricularse en cursos."
                    });
                }

                // Verificar si ya está inscrito
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseOfferingId == courseOfferingId &&
                                             e.StudentId == user.UserId &&
                                             !e.IsSoftDeleted);

                if (existingEnrollment != null)
                {
                    _logger.LogWarning("El estudiante ya está inscrito en este curso: {UserId} - {CourseOfferingId}",
                        user.UserId, courseOfferingId);
                    return Json(new
                    {
                        success = false,
                        message = "Ya estás inscrito en este curso."
                    });
                }

                // Obtener información del curso
                var offering = await _context.CourseOfferings
                    .Include(co => co.Period)
                    .Include(co => co.Course)
                    .FirstOrDefaultAsync(co => co.Id == courseOfferingId && !co.IsSoftDeleted);

                if (offering == null)
                {
                    _logger.LogWarning("Oferta de curso no encontrada: {CourseOfferingId}", courseOfferingId);
                    return Json(new
                    {
                        success = false,
                        message = "El curso no existe o ha sido eliminado."
                    });
                }

                if (!offering.IsActive)
                {
                    _logger.LogWarning("Curso no activo: {CourseOfferingId}", courseOfferingId);
                    return Json(new
                    {
                        success = false,
                        message = "El curso no está disponible para matrícula en este momento."
                    });
                }

                var today = DateTime.Now.Date;
                if (today > offering.Period.EndDate.Date)
                {
                    _logger.LogWarning("Período finalizado: {CourseOfferingId} - {EndDate}",
                        courseOfferingId, offering.Period.EndDate);
                    return Json(new
                    {
                        success = false,
                        message = "El período de matrícula para este curso ha finalizado."
                    });
                }

                if (today < offering.Period.StartDate.Date)
                {
                    _logger.LogWarning("Período no iniciado: {CourseOfferingId} - {StartDate}",
                        courseOfferingId, offering.Period.StartDate);
                    return Json(new
                    {
                        success = false,
                        message = "El período de matrícula para este curso aún no ha iniciado."
                    });
                }

                // Crear la inscripción
                var enrollment = new Enrollments
                {
                    Id = Guid.NewGuid(),
                    CourseOfferingId = courseOfferingId,
                    StudentId = user.UserId,
                    EnrolledAt = DateTime.Now,
                    Status = "Active",
                    IsSoftDeleted = false
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Matrícula exitosa: Estudiante {UserId} en curso {CourseOfferingId}",
                    user.UserId, courseOfferingId);

                // AUDITORÍA: Auto-matrícula de estudiante
                await _auditService.LogAsync("ENROLL_SELF", "Matrícula", enrollment.Id,
                    $"Estudiante {user.Nombre} se auto-matriculó en {offering.Course?.Title} - Sección {offering.Section}");

                return Json(new
                {
                    success = true,
                    message = "¡Matrícula exitosa! Te has inscrito en el curso correctamente."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en matrícula automática para oferta {CourseOfferingId}", courseOfferingId);

                // AUDITORÍA: Error en auto-matrícula
                await _auditService.LogAsync("ENROLL_SELF_ERROR", "Matrícula", null,
                    $"Error en auto-matrícula para oferta {courseOfferingId}: {ex.Message}");

                return Json(new
                {
                    success = false,
                    message = "Error interno al procesar la matrícula. Por favor, intenta nuevamente."
                });
            }
        }

        // ========== GESTIÓN DE DOCUMENTOS ==========

        // GET: CourseOfferings/Documentos/{id}
        public IActionResult Documentos(Guid id)
        {
            // Limpiar mensajes antiguos
            TempData.Remove("Success");
            TempData.Remove("Error");

            var courseOffering = _context.CourseOfferings
                .Include(c => c.Course)
                .FirstOrDefault(c => c.Id == id);

            if (courseOffering == null)
            {
                return NotFound();
            }

            // Verificar permisos
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole == "Estudiante")
            {
                // Verificar si el estudiante está inscrito
                var isEnrolled = _context.Enrollments
                    .Any(e => e.CourseOfferingId == id &&
                             e.StudentId == userId &&
                             !e.IsSoftDeleted);

                if (!isEnrolled)
                {
                    TempData["Error"] = "No tienes acceso a los documentos de este curso";
                    return RedirectToAction("MisCursos");
                }
            }
            else if (userRole == "Profesor" && courseOffering.ProfessorId != userId)
            {
                TempData["Error"] = "No tienes permisos para acceder a los documentos de este curso";
                return RedirectToAction("MisCursos");
            }

            // Obtener lista de archivos del sistema de archivos
            var documents = _fileStorageService.GetCourseDocuments(id);

            ViewBag.CourseOfferingId = id;
            ViewBag.CourseName = courseOffering.Course?.Title ?? "Sin nombre";
            ViewBag.UserRole = userRole;
            ViewBag.IsProfesor = (userRole == "Profesor" && courseOffering.ProfessorId == userId) || userRole == "Administrador";

            return View(documents);
        }

        // POST: CourseOfferings/UploadDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(Guid courseOfferingId, IFormFile file)
        {
            // Verificar permisos - Solo profesor del curso o admin
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var offering = await _context.CourseOfferings
                .Include(co => co.Course)
                .FirstOrDefaultAsync(co => co.Id == courseOfferingId && !co.IsSoftDeleted);

            if (offering == null)
            {
                TempData["Error"] = "No se encontró el curso";
                return RedirectToAction("MisCursos");
            }

            if (userRole != "Administrador" && offering.ProfessorId != userId)
            {
                TempData["Error"] = "Solo el profesor del curso puede subir documentos";
                return RedirectToAction("Documentos", new { id = courseOfferingId });
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar un archivo.";
                return RedirectToAction("Documentos", new { id = courseOfferingId });
            }

            // Validar archivo
            if (!_fileStorageService.ValidateFile(file, out string errorMessage))
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("Documentos", new { id = courseOfferingId });
            }

            try
            {
                // Generar nombre único
                string extension = Path.GetExtension(file.FileName);
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace("(", "")
                    .Replace(")", "");
                string uniqueFileName = $"{timestamp}_{sanitizedFileName}{extension}";

                // Guardar archivo
                await _fileStorageService.SaveCourseDocumentAsync(file, courseOfferingId, uniqueFileName);

                // AUDITORÍA: Documento subido
                await _auditService.LogAsync("DOCUMENT_UPLOAD", "Documento", null,
                    $"Documento '{file.FileName}' subido a {offering.Course?.Title} - Sección {offering.Section}");

                TempData["Success"] = $"Documento '{file.FileName}' subido exitosamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo documento para curso {CourseOfferingId}", courseOfferingId);

                // AUDITORÍA: Error al subir documento
                await _auditService.LogAsync("DOCUMENT_UPLOAD_ERROR", "Documento", null,
                    $"Error al subir documento para oferta {courseOfferingId}: {ex.Message}");

                TempData["Error"] = "Error al subir el archivo.";
            }

            return RedirectToAction("Documentos", new { id = courseOfferingId });
        }

        // GET: CourseOfferings/DownloadDocument
        public async Task<IActionResult> DownloadDocument(Guid courseOfferingId, string fileName)
        {
            try
            {
                // Verificar permisos
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();

                if (userRole == "Estudiante")
                {
                    // Verificar si el estudiante está inscrito
                    var isEnrolled = _context.Enrollments
                        .Any(e => e.CourseOfferingId == courseOfferingId &&
                                 e.StudentId == userId &&
                                 !e.IsSoftDeleted);

                    if (!isEnrolled)
                    {
                        TempData["Error"] = "No tienes permisos para descargar este documento";
                        return RedirectToAction("MisCursos");
                    }
                }
                else if (userRole == "Profesor")
                {
                    // Verificar si es el profesor del curso
                    var offering = _context.CourseOfferings
                        .Include(co => co.Course)
                        .FirstOrDefault(co => co.Id == courseOfferingId && !co.IsSoftDeleted);

                    if (offering?.ProfessorId != userId)
                    {
                        TempData["Error"] = "No tienes permisos para descargar este documento";
                        return RedirectToAction("MisCursos");
                    }
                }

                var filePath = _fileStorageService.GetCourseDocumentPath(courseOfferingId, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "El archivo no existe.";
                    return RedirectToAction("Documentos", new { id = courseOfferingId });
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                // Obtener el nombre original del archivo (sin timestamp)
                var parts = fileName.Split('_', 2);
                var originalFileName = parts.Length > 1 ? parts[1] : fileName;

                // AUDITORÍA: Documento descargado
                await _auditService.LogAsync("DOCUMENT_DOWNLOAD", "Documento", null,
                    $"Documento '{originalFileName}' descargado de oferta {courseOfferingId}");

                return File(memory, GetContentType(filePath), originalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando documento {FileName}", fileName);

                // AUDITORÍA: Error al descargar documento
                await _auditService.LogAsync("DOCUMENT_DOWNLOAD_ERROR", "Documento", null,
                    $"Error al descargar documento {fileName}: {ex.Message}");

                TempData["Error"] = "Error al descargar el archivo.";
                return RedirectToAction("Documentos", new { id = courseOfferingId });
            }
        }

        // POST: CourseOfferings/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(Guid courseOfferingId, string fileName)
        {
            // Verificar permisos - Solo profesor del curso o admin
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var offering = await _context.CourseOfferings
                .Include(co => co.Course)
                .FirstOrDefaultAsync(co => co.Id == courseOfferingId && !co.IsSoftDeleted);

            if (offering == null)
            {
                TempData["Error"] = "No se encontró el curso";
                return RedirectToAction("MisCursos");
            }

            if (userRole != "Administrador" && offering.ProfessorId != userId)
            {
                TempData["Error"] = "Solo el profesor del curso puede eliminar documentos";
                return RedirectToAction("Documentos", new { id = courseOfferingId });
            }

            try
            {
                bool deleted = _fileStorageService.DeleteCourseDocument(courseOfferingId, fileName);

                if (deleted)
                {
                    // AUDITORÍA: Documento eliminado
                    await _auditService.LogAsync("DOCUMENT_DELETE", "Documento", null,
                        $"Documento '{fileName}' eliminado de {offering.Course?.Title} - Sección {offering.Section}");

                    TempData["Success"] = "Documento eliminado exitosamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el archivo.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando documento {FileName}", fileName);

                // AUDITORÍA: Error al eliminar documento
                await _auditService.LogAsync("DOCUMENT_DELETE_ERROR", "Documento", null,
                    $"Error al eliminar documento {fileName}: {ex.Message}");

                TempData["Error"] = "Error al eliminar el archivo.";
            }

            return RedirectToAction("Documentos", new { id = courseOfferingId });
        }

        // ========== ACCIONES ADICIONALES ==========

        // POST: CourseOfferings/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var offering = await _context.CourseOfferings
                .Include(co => co.Course)
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (offering == null) return NotFound();

            // Verificar permisos - Solo admin o profesor del curso
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole != "Administrador" && offering.ProfessorId != userId)
            {
                TempData["Error"] = "No tiene permisos para cambiar el estado de este curso";
                return RedirectToAction("MisCursos");
            }

            var oldStatus = offering.IsActive;
            offering.IsActive = !offering.IsActive;
            await _context.SaveChangesAsync();

            // AUDITORÍA: Estado de oferta cambiado
            await _auditService.LogAsync("OFFERING_TOGGLE_STATUS", "OfertaCurso", offering.Id,
                $"Oferta {offering.Course?.Title} - Sección {offering.Section} " +
                $"cambió de {(oldStatus ? "Activa" : "Inactiva")} a {(offering.IsActive ? "Activa" : "Inactiva")}");

            var status = offering.IsActive ? "activada" : "desactivada";
            TempData["Success"] = $"Oferta {status} exitosamente";

            // Redirigir según el rol
            if (userRole == "Administrador")
            {
                return RedirectToAction("ListadoOfertas");
            }
            else
            {
                return RedirectToAction("MisCursos");
            }
        }

        // ========== MÉTODOS PRIVADOS ==========

        private async Task LoadDropdownsAsync(CourseOfferingVm? model = null)
        {
            ViewBag.Courses = await _context.Courses
                .Where(c => !c.IsSoftDeleted && c.IsActive)
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Title}",
                    Selected = model != null && c.Id == model.CourseId
                })
                .ToListAsync();

            ViewBag.Professors = await _context.Users
                .Where(u => !u.IsSoftDeleted && u.UserRoles.Any(ur => ur.Role.RoleName == "Profesor"))
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = $"{u.UserName} - {u.Email}",
                    Selected = model != null && u.UserId == model.ProfessorId
                })
                .ToListAsync();

            ViewBag.Periods = await _context.Periods
                .Where(p => !p.IsSoftDeleted && (p.IsActive || p.StartDate >= DateTime.Now.Date))
                .OrderByDescending(p => p.StartDate)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} ({p.StartDate:MMM yyyy} - {p.EndDate:MMM yyyy})",
                    Selected = model != null && p.Id == model.PeriodId
                })
                .ToListAsync();
        }

        #region Helper Methods

        private string GetCurrentUserRole()
        {
            var sesionBase64 = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(sesionBase64))
                return null;

            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(sesionBase64);
                var sesion = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                var userInfo = System.Text.Json.JsonSerializer.Deserialize<UserVm>(sesion);
                return userInfo?.Rol?.Nombre ?? "Estudiante";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo rol del usuario");
                return "Estudiante";
            }
        }

        private Guid GetCurrentUserId()
        {
            var sesionBase64 = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(sesionBase64))
                return Guid.Empty;

            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(sesionBase64);
                var sesion = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                var userInfo = System.Text.Json.JsonSerializer.Deserialize<UserVm>(sesion);
                return userInfo?.UserId ?? Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ID del usuario");
                return Guid.Empty;
            }
        }

        private bool IsAdmin()
        {
            return GetCurrentUserRole() == "Administrador";
        }

        private bool IsAdminFromSession()
        {
            var role = GetCurrentUserRole();
            return role == "Administrador";
        }

        private bool IsProfesorFromSession()
        {
            var role = GetCurrentUserRole();
            return role == "Profesor";
        }

        private bool IsEstudianteFromSession()
        {
            var role = GetCurrentUserRole();
            return role == "Estudiante";
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".pdf", "application/pdf"},
                {".doc", "application/msword"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".ppt", "application/vnd.ms-powerpoint"},
                {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".txt", "text/plain"},
                {".zip", "application/zip"}
            };

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        private async Task<string> GetCourseName(Guid courseId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);
            return course?.Title ?? "Curso desconocido";
        }

        private async Task<string> GetStudentName(Guid studentId)
        {
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == studentId);
            return student?.UserName ?? "Estudiante desconocido";
        }

        private string CalculateStatus(DateTime? startDate, DateTime? endDate, bool isActive)
        {
            if (!isActive)
                return "Inactivo";

            if (!startDate.HasValue || !endDate.HasValue)
                return "Indefinido";

            var today = DateTime.Now.Date;

            if (today < startDate.Value.Date)
                return "Próximo";
            else if (today >= startDate.Value.Date && today <= endDate.Value.Date)
                return "En Curso";
            else
                return "Finalizado";
        }

        #endregion
    }
}