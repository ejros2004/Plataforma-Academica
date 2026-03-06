using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Control_De_Tareas.Controllers
{
    public class CursosController : Controller
    {
        private ContextDB _context;
        private ILogger<CursosController> _logger;
        private readonly AuditService _auditService;

        public CursosController(ContextDB context, ILogger<CursosController> logger, AuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Cursos/Index - Muestra TODAS las ofertas de cursos
        public async Task<IActionResult> Index()
        {
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            IQueryable<CourseOfferings> query = _context.CourseOfferings
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                .Where(co => !co.IsSoftDeleted);

            // Filtrar según el rol - CORREGIDO
            if (userRole == "Profesor")
            {
                // Profesores: Solo cursos que imparten Y que están activos
                query = query.Where(co => co.ProfessorId == userId && co.IsActive);
            }
            else if (userRole == "Estudiante")
            {
                // Estudiantes: Solo cursos en los que están inscritos Y que están activos
                query = query.Where(co => co.IsActive &&
                                       co.Enrollments.Any(e => e.StudentId == userId &&
                                                             !e.IsSoftDeleted &&
                                                             e.Status == "Active"));
            }
            // Administradores pueden ver todo (activos e inactivos)

            var cursos = await query
                .Select(co => new
                {
                    CourseOffering = co,
                    Course = co.Course,
                    Professor = co.Professor,
                    Period = co.Period,
                    EnrollmentCount = co.Enrollments.Count(e => !e.IsSoftDeleted)
                })
                .OrderByDescending(x => x.CourseOffering.IsActive) // Activos primero
                .ThenBy(x => x.Course.Title)
                .ToListAsync();

            var cursoDtos = cursos.Select(x => new CursoDto
            {
                Id = x.CourseOffering.Id,  // ID del CourseOffering
                CursoId = x.CourseOffering.CourseId, // ID del Course base
                Codigo = x.Course.Code ?? "",
                Nombre = x.Course.Title,
                Descripcion = x.Course.Description ?? "",
                Estado = x.CourseOffering.IsActive ? "Activo" : "Inactivo",
                InstructorNombre = x.Professor.UserName ?? "Sin asignar",
                CantidadEstudiantes = x.EnrollmentCount,
                Seccion = x.CourseOffering.Section ?? "",
                Periodo = x.Period.Name ?? "Sin período"
            }).ToList();

            var viewModel = new CursosVm
            {
                Cursos = cursoDtos
            };

            // Determinar rol del usuario para la vista
            ViewBag.UserRole = userRole;
            ViewBag.IsAdmin = userRole == "Administrador";
            ViewBag.IsProfesor = userRole == "Profesor";
            ViewBag.IsEstudiante = userRole == "Estudiante";

            return View(viewModel);
        }

        // GET: Cursos/Crear - Crear CURSO BASE (no oferta)
        [Authorize(Roles = "Administrador")]
        public IActionResult Crear()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden crear cursos";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Crear(string Codigo, string Nombre, string Descripcion)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden crear cursos";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(Codigo) || string.IsNullOrEmpty(Nombre))
            {
                TempData["Error"] = "Código y Nombre son requeridos";
                return View();
            }

            // Verificar si ya existe un curso con el mismo código
            var existe = await _context.Courses
                .AnyAsync(c => !c.IsSoftDeleted && c.Code == Codigo);

            if (existe)
            {
                TempData["Error"] = "Ya existe un curso con este código";
                return View();
            }

            var curso = new Courses
            {
                Id = Guid.NewGuid(),
                Code = Codigo,
                Title = Nombre,
                Description = Descripcion,
                CreatedAt = DateTime.Now,
                IsActive = true,
                IsSoftDeleted = false
            };

            _context.Courses.Add(curso);
            await _context.SaveChangesAsync();

            // AUDITORÍA: Curso creado
            await _auditService.LogCreateAsync("Curso", curso.Id, $"{Codigo} - {Nombre}");

            TempData["Success"] = "Curso creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Cursos/MenuDetalles - Detalles específicos de una oferta
        public async Task<IActionResult> MenuDetalles(Guid id)
        {
            var courseOffering = await _context.CourseOfferings
                .Include(co => co.Course)
                .Include(co => co.Professor)
                .Include(co => co.Period)
                .Include(co => co.Enrollments)
                .FirstOrDefaultAsync(co => co.Id == id && !co.IsSoftDeleted);

            if (courseOffering == null)
            {
                return NotFound();
            }

            // Verificar rol del usuario
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            // Determinar permisos
            var esAdmin = userRole == "Administrador";
            var esProfesorDelCurso = userRole == "Profesor" && courseOffering.ProfessorId == userId;
            var estaInscrito = courseOffering.Enrollments?
                .Any(e => e.StudentId == userId && !e.IsSoftDeleted) ?? false;

            // Preparar el modelo
            var model = new DetallesCursosVM
            {
                Id = courseOffering.Id,
                Nombre = courseOffering.Course.Title,
                Descripcion = courseOffering.Course.Description ?? string.Empty,

                // Conteos reales (solo para quienes tienen acceso)
                TotalTareas = esAdmin || esProfesorDelCurso || estaInscrito
                    ? await _context.Tareas.CountAsync(t => t.CourseOfferingId == id && !t.IsSoftDeleted)
                    : 0,

                TotalDocumentos = 0,
                TotalEnlaces = 1,
                TotalAnuncios = esAdmin || esProfesorDelCurso || estaInscrito
                    ? await _context.Announcements.CountAsync(a => a.CourseOfferingId == id && !a.IsSoftDeleted)
                    : 0,
                TotalEvaluaciones = 0,

                // Permisos específicos
                EsAdmin = esAdmin,
                EsProfesor = esProfesorDelCurso,
                EsEstudiante = estaInscrito
            };

            // Agregar información para la vista
            ViewBag.PuedeAccederContenido = esAdmin || esProfesorDelCurso || estaInscrito;
            ViewBag.EstaInscrito = estaInscrito;
            ViewBag.PuedeMatricularse = userRole == "Estudiante" && !estaInscrito &&
                                       courseOffering.IsActive &&
                                       courseOffering.Period?.EndDate >= DateTime.Now.Date;
            ViewBag.UserRole = userRole;
            ViewBag.CourseOffering = courseOffering;

            return View("DetallesMenu", model);
        }

        // GET: Cursos/Editar - Editar CURSO BASE (solo admin)
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(Guid id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden editar cursos";
                return RedirectToAction("Index");
            }

            var curso = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsSoftDeleted);

            if (curso == null)
            {
                return NotFound();
            }

            var model = new CursoDto
            {
                Id = curso.Id,
                Codigo = curso.Code,
                Nombre = curso.Title,
                Descripcion = curso.Description ?? "",
                Estado = curso.IsActive ? "Activo" : "Inactivo"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(CursoDto model)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden editar cursos";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var curso = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsSoftDeleted);

            if (curso == null)
            {
                return NotFound();
            }

            // Verificar si el código ya existe en otro curso
            var existe = await _context.Courses
                .AnyAsync(c => !c.IsSoftDeleted && c.Id != model.Id && c.Code == model.Codigo);

            if (existe)
            {
                ModelState.AddModelError("Codigo", "Ya existe otro curso con este código");
                return View(model);
            }

            // Guardar valores antiguos para auditoría
            var oldValues = new
            {
                curso.Code,
                curso.Title,
                curso.Description,
                curso.IsActive
            };

            curso.Code = model.Codigo;
            curso.Title = model.Nombre;
            curso.Description = model.Descripcion;
            curso.IsActive = model.Estado == "Activo";

            await _context.SaveChangesAsync();

            // AUDITORÍA: Curso actualizado
            await _auditService.LogUpdateAsync("Curso", curso.Id, $"{model.Codigo} - {model.Nombre}");

            TempData["Success"] = "Curso actualizado exitosamente";
            return RedirectToAction("Index");
        }

        // GET: Cursos/Eliminar - Eliminar CURSO BASE (solo admin)
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden eliminar cursos";
                return RedirectToAction("Index");
            }

            var curso = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsSoftDeleted);

            if (curso == null)
            {
                return NotFound();
            }

            // Verificar si tiene ofertas asociadas
            var tieneOfertas = await _context.CourseOfferings
                .AnyAsync(co => co.CourseId == id && !co.IsSoftDeleted);

            var model = new CursoDto
            {
                Id = curso.Id,
                Codigo = curso.Code,
                Nombre = curso.Title,
                Descripcion = curso.Description ?? "",
                Estado = curso.IsActive ? "Activo" : "Inactivo"
            };

            ViewBag.TieneOfertas = tieneOfertas;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarConfirmado(Guid id)
        {
            try
            {
                // 1. Buscar el curso
                var curso = await _context.Courses.FindAsync(id);

                if (curso == null)
                {
                    TempData["Error"] = "Curso no encontrado";
                    return RedirectToAction("GestionarCursosBase");
                }

                // 2. Verificar si tiene ofertas activas
                bool tieneOfertas = await _context.CourseOfferings
                    .AnyAsync(co => co.CourseId == id && !co.IsSoftDeleted);

                if (tieneOfertas)
                {
                    // AUDITORÍA: Intento de eliminar curso con ofertas
                    await _auditService.LogAsync("CURSO_DELETE_WITH_OFFERINGS", "Curso", curso.Id,
                        $"Intento de eliminar curso {curso.Code} - {curso.Title} con ofertas activas");

                    TempData["Error"] = "No se puede eliminar: tiene ofertas activas";
                    return RedirectToAction("Eliminar", new { id });
                }

                // 3. Eliminar (soft delete)
                curso.IsSoftDeleted = true;
                curso.IsActive = false;

                // 4. Guardar
                await _context.SaveChangesAsync();

                // AUDITORÍA: Curso eliminado
                await _auditService.LogDeleteAsync("Curso", curso.Id, $"{curso.Code} - {curso.Title}");

                // 5. Mensaje y redirigir
                TempData["Success"] = $"Curso '{curso.Title}' eliminado correctamente";
                return RedirectToAction("GestionarCursosBase");
            }
            catch (Exception ex)
            {
                // AUDITORÍA: Error al eliminar curso
                await _auditService.LogAsync("CURSO_DELETE_ERROR", "Curso", id,
                    $"Error al eliminar curso: {ex.Message}");

                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Eliminar", new { id });
            }
        }

        // ========== ACTIVAR/DESACTIVAR CURSO ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ToggleEstado(Guid id)
        {
            try
            {
                if (!IsAdmin())
                {
                    TempData["Error"] = "Solo los administradores pueden cambiar el estado de cursos";
                    return RedirectToAction(nameof(Index));
                }

                var curso = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsSoftDeleted);

                if (curso == null)
                {
                    return NotFound();
                }

                var estadoAnterior = curso.IsActive;
                curso.IsActive = !curso.IsActive;
                await _context.SaveChangesAsync();

                // AUDITORÍA: Estado del curso cambiado
                await _auditService.LogAsync("CURSO_TOGGLE_ESTADO", "Curso", curso.Id,
                    $"Curso {curso.Code} - {curso.Title} cambió de " +
                    $"{(estadoAnterior ? "Activo" : "Inactivo")} a {(curso.IsActive ? "Activo" : "Inactivo")}");

                var estado = curso.IsActive ? "activado" : "desactivado";
                TempData["Success"] = $"Curso {estado} exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del curso {Id}", id);

                // AUDITORÍA: Error al cambiar estado
                await _auditService.LogAsync("CURSO_TOGGLE_ESTADO_ERROR", "Curso", id,
                    $"Error al cambiar estado del curso: {ex.Message}");

                TempData["Error"] = $"Error al cambiar el estado del curso: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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
            catch
            {
                return Guid.Empty;
            }
        }

        // GET: Cursos/GestionarCursosBase - CRUD de cursos base (solo admin)
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GestionarCursosBase()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Solo los administradores pueden gestionar cursos base";
                return RedirectToAction("Index");
            }

            var cursos = await _context.Courses
                .Where(c => !c.IsSoftDeleted)
                .OrderBy(c => c.Code)
                .Select(c => new CourseBaseVm
                {
                    Id = c.Id,
                    Code = c.Code,
                    Title = c.Title,
                    Description = c.Description ?? "",
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    HasOfferings = _context.CourseOfferings.Any(co => co.CourseId == c.Id && !co.IsSoftDeleted)
                })
                .ToListAsync();

            return View(cursos);
        }

        private bool IsAdmin()
        {
            return GetCurrentUserRole() == "Administrador";
        }

        #endregion
    }
}