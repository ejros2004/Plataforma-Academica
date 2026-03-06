using Microsoft.AspNetCore.Mvc;
using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace Control_De_Tareas.Controllers
{
    public class SubmissionsController : Controller
    {
        private readonly ContextDB _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly AuditService _auditService;
        private readonly ILogger<SubmissionsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment; // AÑADIDO

        public SubmissionsController(ContextDB context, IFileStorageService fileStorageService,
            AuditService auditService, ILogger<SubmissionsController> logger,
            IWebHostEnvironment webHostEnvironment) // AÑADIDO
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _auditService = auditService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment; // AÑADIDO
        }

        // ============================
        // INDEX
        // ============================
        public async Task<IActionResult> Index()
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            IQueryable<Submissions> query = _context.Submissions
                .Include(s => s.Task)
                    .ThenInclude(t => t.CourseOffering)
                .Include(s => s.Student)
                .Include(s => s.SubmissionFiles)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Grader)
                .Where(s => !s.IsSoftDeleted);

            if (userInfo.Rol?.Nombre == "Estudiante")
            {
                query = query.Where(s => s.StudentId == userInfo.UserId);
            }
            else if (userInfo.Rol?.Nombre == "Profesor")
            {
                query = query.Where(s => s.Task.CourseOffering.ProfessorId == userInfo.UserId);
            }

            var submissions = await query
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            ViewBag.UserRole = userInfo.Rol?.Nombre;
            ViewBag.UserId = userInfo.UserId;
            ViewBag.UserName = userInfo.Nombre;

            return View(submissions);
        }

        // ============================
        // DETAILS
        // ============================
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            var submission = await _context.Submissions
                .Include(s => s.Task)
                    .ThenInclude(t => t.CourseOffering)
                        .ThenInclude(co => co.Course)
                .Include(s => s.Student)
                .Include(s => s.SubmissionFiles)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Grader)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsSoftDeleted);

            if (submission == null)
                return NotFound();

            if (userInfo.Rol?.Nombre == "Estudiante" && submission.StudentId != userInfo.UserId)
            {
                TempData["Error"] = "No tienes permisos para ver esta entrega.";
                return RedirectToAction("Index");
            }

            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = submission.Task?.CourseOffering?.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    var cursoNombre = submission.Task?.CourseOffering?.Course?.Title ?? "curso desconocido";
                    TempData["Error"] = $"No tienes permisos para ver esta entrega. Esta entrega pertenece al curso '{cursoNombre}'.";
                    return RedirectToAction("Index");
                }
            }

            return View(submission);
        }

        // ============================
        // CREATE GET
        // ============================
        [HttpGet]
        public async Task<IActionResult> Create(Guid? taskId)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            var model = new SubmissionCreateVm();

            if (taskId.HasValue)
            {
                var task = await _context.Tareas.FirstOrDefaultAsync(t => t.Id == taskId.Value && !t.IsSoftDeleted);
                if (task != null)
                {
                    model.TaskId = task.Id;
                    model.TaskTitle = task.Title;
                    model.TaskDueDate = task.DueDate;
                }
            }

            ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
            return View(model);
        }

        // ============================
        // CREATE POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmissionCreateVm model)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("TaskTitle");
            ModelState.Remove("TaskDueDate");

            if (!ModelState.IsValid)
            {
                ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
                return View(model);
            }

            try
            {
                var task = await _context.Tareas
                    .Include(t => t.CourseOffering)
                    .FirstOrDefaultAsync(t => t.Id == model.TaskId && !t.IsSoftDeleted);

                if (task == null)
                {
                    ModelState.AddModelError("", "La tarea seleccionada no existe.");
                    ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
                    return View(model);
                }

                var isEnrolled = await _context.Enrollments.AnyAsync(e =>
                    e.StudentId == userInfo.UserId &&
                    e.CourseOfferingId == task.CourseOfferingId &&
                    !e.IsSoftDeleted &&
                    e.Status == "Active");

                if (!isEnrolled)
                {
                    ModelState.AddModelError("", "No estás inscrito en este curso.");
                    ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
                    return View(model);
                }

                if (DateTime.Now > task.DueDate)
                {
                    ModelState.AddModelError("", "La fecha límite ya venció.");
                    ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
                    return View(model);
                }

                var submission = new Submissions
                {
                    Id = Guid.NewGuid(),
                    StudentId = userInfo.UserId,
                    TaskId = model.TaskId,
                    SubmittedAt = DateTime.Now,
                    Comments = model.Comments,
                    Status = "Submitted",
                    CurrentGrade = 0,
                    IsSoftDeleted = false
                };

                _context.Submissions.Add(submission);

                int filesCount = 0;

                if (model.Files != null && model.Files.Any())
                {
                    filesCount = model.Files.Count;

                    foreach (var file in model.Files)
                    {
                        if (!_fileStorageService.ValidateFile(file, out string errorMessage))
                        {
                            ModelState.AddModelError("Files", errorMessage);
                            ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
                            return View(model);
                        }

                        string extension = Path.GetExtension(file.FileName);
                        string uniqueFileName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}{extension}";

                        int courseOfferingIdInt = Math.Abs(task.CourseOffering.GetHashCode());
                        int taskIdInt = Math.Abs(task.Id.GetHashCode());

                        string filePath = await _fileStorageService.SaveFileAsync(
                            file, courseOfferingIdInt, taskIdInt, uniqueFileName);

                        // CORRECCIÓN: Verificar si el archivo se guardó correctamente
                        _logger.LogInformation($"Archivo guardado: {file.FileName}, Ruta: {filePath}");
                        if (!System.IO.File.Exists(filePath))
                        {
                            ModelState.AddModelError("Files", "Error al guardar el archivo en el servidor.");
                            ViewBag.Tasks = await GetAvailableTasksForStudent(userInfo.UserId);
                            return View(model);
                        }

                        var submissionFile = new SubmissionFiles
                        {
                            Id = Guid.NewGuid(),
                            SubmissionId = submission.Id,
                            FileName = file.FileName,
                            FilePath = filePath,
                            UploadedAt = DateTime.Now,
                            IsSoftDeleted = false
                        };

                        _context.SubmissionFiles.Add(submissionFile);
                    }
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("SUBMISSION_CREATE", "Entrega", submission.Id,
                    $"Entrega creada con {filesCount} archivos");

                TempData["Success"] = "✅ Entrega creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await _auditService.LogAsync("SUBMISSION_ERROR", "Entrega", null, ex.Message);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================
        // DELETE ENTREGA
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("=== INICIANDO ELIMINACIÓN DE ENTREGA ===");
            _logger.LogInformation("ID de entrega: {SubmissionId}", id);

            try
            {
                var userInfo = GetCurrentUser();
                if (userInfo == null)
                {
                    _logger.LogWarning("Usuario no autenticado intentando eliminar");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("Usuario: {Nombre} ({Rol})", userInfo.Nombre, userInfo.Rol?.Nombre);

                if (userInfo.Rol?.Nombre != "Profesor" && userInfo.Rol?.Nombre != "Administrador")
                {
                    TempData["Error"] = "No tienes permisos para eliminar entregas.";
                    _logger.LogWarning("Usuario sin permisos intentando eliminar entrega");
                    return RedirectToAction(nameof(Index));
                }

                var submission = await _context.Submissions
                    .Include(s => s.Task)
                        .ThenInclude(t => t.CourseOffering)
                            .ThenInclude(co => co.Course)
                    .Include(s => s.Student)
                    .Include(s => s.SubmissionFiles)
                    .Include(s => s.Grades)
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsSoftDeleted);

                if (submission == null)
                {
                    TempData["Error"] = "La entrega no existe o ya fue eliminada.";
                    _logger.LogWarning("Entrega no encontrada: {SubmissionId}", id);
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Entrega encontrada: Estudiante={StudentId}, Tarea={TaskTitle}",
                    submission.StudentId, submission.Task?.Title);

                if (userInfo.Rol?.Nombre == "Profesor")
                {
                    var esProfesorDelCurso = submission.Task?.CourseOffering?.ProfessorId == userInfo.UserId;
                    if (!esProfesorDelCurso)
                    {
                        var cursoNombre = submission.Task?.CourseOffering?.Course?.Title ?? "curso desconocido";
                        var estudianteNombre = submission.Student?.UserName ?? "estudiante";
                        var tareaTitulo = submission.Task?.Title ?? "tarea";

                        TempData["Error"] = $"No tienes permisos para eliminar esta entrega. " +
                                          $"Esta entrega pertenece a la tarea '{tareaTitulo}' del curso '{cursoNombre}', " +
                                          $"y tú no eres el profesor de ese curso.";

                        _logger.LogWarning("Profesor {UserId} no es del curso {CourseId} - {CourseName}",
                            userInfo.UserId, submission.Task?.CourseOfferingId, cursoNombre);
                        return RedirectToAction(nameof(Index));
                    }
                }

                _logger.LogInformation("Iniciando soft delete de entrega...");

                submission.IsSoftDeleted = true;

                int archivosEliminados = 0;
                foreach (var file in submission.SubmissionFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(file.FilePath))
                        {
                            System.IO.File.Delete(file.FilePath);
                            archivosEliminados++;
                            _logger.LogInformation("Archivo físico eliminado: {FilePath}", file.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo eliminar el archivo físico: {FilePath}", file.FilePath);
                    }

                    file.IsSoftDeleted = true;
                }

                int calificacionesEliminadas = 0;
                foreach (var grade in submission.Grades)
                {
                    grade.IsSoftDeleted = true;
                    calificacionesEliminadas++;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("SUBMISSION_DELETE", "Entrega", submission.Id,
                    $"Entrega eliminada por {userInfo.Nombre}. Archivos: {archivosEliminados}, Calificaciones: {calificacionesEliminadas}");

                _logger.LogInformation("=== ELIMINACIÓN EXITOSA ===");
                _logger.LogInformation("Archivos eliminados: {ArchivosEliminados}", archivosEliminados);
                _logger.LogInformation("Calificaciones eliminadas: {CalificacionesEliminadas}", calificacionesEliminadas);

                TempData["Success"] = "✅ Entrega eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CRÍTICO eliminando entrega {SubmissionId}", id);
                TempData["Error"] = $"Error al eliminar la entrega: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================
        // REVIEW POR TAREA
        // ============================
        [HttpGet]
        public async Task<IActionResult> Review(Guid taskId)
        {
            var userInfo = GetCurrentUser();

            if (userInfo == null ||
                (userInfo.Rol?.Nombre != "Profesor" && userInfo.Rol?.Nombre != "Administrador"))
            {
                return RedirectToAction("Login", "Account");
            }

            var task = await _context.Tareas
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsSoftDeleted);

            if (task == null)
                return NotFound();

            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = task.CourseOffering.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    TempData["Error"] = $"No tienes permisos para revisar esta tarea. Esta tarea pertenece al curso '{task.CourseOffering.Course?.Title}'.";
                    return RedirectToAction("Index", "Tareas");
                }
            }

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.SubmissionFiles)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Grader)
                .Where(s => s.TaskId == taskId && !s.IsSoftDeleted)
                .OrderBy(s => s.Student.UserName)
                .ToListAsync();

            ViewBag.TaskTitle = task.Title;
            ViewBag.MaxScore = task.MaxScore;

            return View(submissions);
        }

        // ============================
        // CALIFICAR ENTREGA
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(Guid id, decimal grade, string feedback)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null ||
                (userInfo.Rol?.Nombre != "Profesor" && userInfo.Rol?.Nombre != "Administrador"))
                return RedirectToAction("Login", "Account");

            if (grade < 0 || grade > 100)
            {
                TempData["Error"] = "La nota debe estar entre 0 y 100.";
                return RedirectToAction(nameof(Index));
            }

            var submission = await _context.Submissions
                .Include(s => s.Task)
                    .ThenInclude(t => t.CourseOffering)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsSoftDeleted);

            if (submission == null)
                return NotFound();

            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = submission.Task?.CourseOffering?.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    var cursoNombre = submission.Task?.CourseOffering?.Course?.Title ?? "curso desconocido";
                    TempData["Error"] = $"No tienes permisos para calificar esta entrega. Esta entrega pertenece al curso '{cursoNombre}'.";
                    return RedirectToAction(nameof(Index));
                }
            }

            submission.CurrentGrade = grade;
            submission.Status = "Calificada";

            var gradeEntity = new Grades
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                GraderId = userInfo.UserId,
                Score = grade,
                Feedback = feedback,
                GradedAt = DateTime.Now,
                IsSoftDeleted = false
            };

            _context.Grades.Add(gradeEntity);

            await _auditService.LogAsync("SUBMISSION_GRADE", "Entrega", submission.Id,
                $"Entrega calificada con {grade}");

            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Entrega calificada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // DESCARGAR ARCHIVO DE ENTREGA - VERSIÓN CORREGIDA
        // ============================
        [HttpGet]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            try
            {
                _logger.LogInformation("=== INICIANDO DESCARGA DE ARCHIVO ===");
                _logger.LogInformation("FileId: {FileId}", fileId);

                var userInfo = GetCurrentUser();
                if (userInfo == null)
                {
                    _logger.LogWarning("Usuario no autenticado");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("Usuario: {Nombre} ({Rol})", userInfo.Nombre, userInfo.Rol?.Nombre);

                var submissionFile = await _context.SubmissionFiles
                    .Include(f => f.Submission)
                        .ThenInclude(s => s.Student)
                    .Include(f => f.Submission)
                        .ThenInclude(s => s.Task)
                            .ThenInclude(t => t.CourseOffering)
                                .ThenInclude(co => co.Course)
                    .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsSoftDeleted);

                if (submissionFile == null)
                {
                    _logger.LogError("Archivo de entrega no encontrado en la base de datos");
                    return NotFound("El archivo no existe o ha sido eliminado.");
                }

                _logger.LogInformation("Archivo encontrado: {FileName}", submissionFile.FileName);

                // Verificar permisos de seguridad
                var userRole = userInfo.Rol?.Nombre;
                var userId = userInfo.UserId;

                if (userRole == "Estudiante")
                {
                    if (submissionFile.Submission?.StudentId != userId)
                    {
                        _logger.LogWarning("Estudiante intentando descargar archivo ajeno");
                        TempData["Error"] = "No tienes permisos para descargar este archivo.";
                        return RedirectToAction("Index");
                    }
                }

                if (userRole == "Profesor")
                {
                    var task = submissionFile.Submission?.Task;
                    if (task == null || task.CourseOffering == null)
                    {
                        _logger.LogError("No se pudo obtener información del curso");
                        TempData["Error"] = "Error al verificar permisos.";
                        return RedirectToAction("Index");
                    }

                    if (task.CourseOffering.ProfessorId != userId)
                    {
                        var cursoNombre = task.CourseOffering.Course?.Title ?? "curso desconocido";
                        _logger.LogWarning("Profesor no es del curso {CourseName}. Acceso denegado.", cursoNombre);
                        TempData["Error"] = $"Solo el profesor del curso '{cursoNombre}' puede descargar estas entregas.";
                        return RedirectToAction("Index");
                    }
                }

                if (userRole != "Administrador" && userRole != "Profesor" && userRole != "Estudiante")
                {
                    _logger.LogWarning("Rol desconocido: {UserRole}", userRole);
                    TempData["Error"] = "No tienes permisos para descargar este archivo.";
                    return RedirectToAction("Index");
                }

                // Obtener ruta del archivo
                var filePath = submissionFile.FilePath;
                _logger.LogInformation("FilePath original: {FilePath}", filePath);

                // Convertir ruta relativa a absoluta si es necesario
                if (!Path.IsPathRooted(filePath))
                {
                    if (filePath.StartsWith("~/") || filePath.StartsWith("/"))
                    {
                        filePath = filePath.TrimStart('~', '/');
                    }
                    filePath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);
                }

                _logger.LogInformation("FilePath absoluto: {FilePath}", filePath);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError("ARCHIVO NO ENCONTRADO EN EL SERVER: {FilePath}", filePath);
                    return NotFound($"El archivo '{submissionFile.FileName}' no existe en el servidor.");
                }

                // CORRECCIÓN CLAVE: Usar FileStreamResult en lugar de File con bytes
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var contentType = GetContentType(submissionFile.FileName);

                _logger.LogInformation("Content-Type: {ContentType}", contentType);
                _logger.LogInformation("=== DESCARGA EXITOSA ===");

                // Registrar auditoría
                await _auditService.LogAsync("SUBMISSION_FILE_DOWNLOAD", "ArchivoEntrega", submissionFile.Id,
                    $"Archivo descargado: {submissionFile.FileName} por {userInfo.Nombre} ({userRole})");

                // Retornar FileStreamResult - esto evita corrupción de archivos
                return new FileStreamResult(fileStream, contentType)
                {
                    FileDownloadName = submissionFile.FileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CRÍTICO descargando archivo {FileId}", fileId);

                await _auditService.LogAsync("SUBMISSION_FILE_DOWNLOAD_ERROR", "ArchivoEntrega", fileId,
                    $"Error al descargar archivo: {ex.Message}");

                TempData["Error"] = $"Error al descargar el archivo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ============================
        // REABRIR ENTREGA
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(Guid id)
        {
            var userInfo = GetCurrentUser();
            if (userInfo == null ||
                (userInfo.Rol?.Nombre != "Profesor" && userInfo.Rol?.Nombre != "Administrador"))
                return RedirectToAction("Login", "Account");

            var submission = await _context.Submissions
                .Include(s => s.Task)
                    .ThenInclude(t => t.CourseOffering)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsSoftDeleted);

            if (submission == null)
                return NotFound();

            if (userInfo.Rol?.Nombre == "Profesor")
            {
                var esProfesorDelCurso = submission.Task?.CourseOffering?.ProfessorId == userInfo.UserId;
                if (!esProfesorDelCurso)
                {
                    var cursoNombre = submission.Task?.CourseOffering?.Course?.Title ?? "curso desconocido";
                    TempData["Error"] = $"No tienes permisos para reabrir esta entrega. Esta entrega pertenece al curso '{cursoNombre}'.";
                    return RedirectToAction(nameof(Index));
                }
            }

            submission.Status = "Submitted";
            submission.CurrentGrade = null;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("SUBMISSION_REOPEN", "Entrega", submission.Id,
                "Entrega reabierta para recalificación");

            TempData["Success"] = "✅ Entrega reabierta correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // HELPERS
        // ============================
        private UserVm GetCurrentUser()
        {
            var sesionBase64 = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(sesionBase64)) return null;

            var base64EncodedBytes = Convert.FromBase64String(sesionBase64);
            var sesion = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            return JsonConvert.DeserializeObject<UserVm>(sesion);
        }

        private async Task<List<TaskSelectVm>> GetAvailableTasksForStudent(Guid studentId)
        {
            return await _context.Tareas
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Course)
                .Include(t => t.CourseOffering)
                    .ThenInclude(co => co.Enrollments)
                .Where(t => !t.IsSoftDeleted &&
                            t.CourseOffering.Enrollments
                                .Any(e => e.StudentId == studentId && !e.IsSoftDeleted))
                .OrderByDescending(t => t.DueDate)
                .Select(t => new TaskSelectVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    DueDate = t.DueDate,
                    CourseName = t.CourseOffering.Course.Title
                }).ToListAsync();
        }

        // ============================
        // OBTENER CONTENT TYPE
        // ============================
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            var mimeTypes = new Dictionary<string, string>
            {
                {".pdf", "application/pdf"},
                {".doc", "application/msword"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".txt", "text/plain"},
                {".rtf", "application/rtf"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".png", "image/png"},
                {".gif", "image/gif"},
                {".bmp", "image/bmp"},
                {".zip", "application/zip"},
                {".rar", "application/x-rar-compressed"},
                {".7z", "application/x-7z-compressed"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".ppt", "application/vnd.ms-powerpoint"},
                {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                {".csv", "text/csv"},
                {".html", "text/html"},
                {".htm", "text/html"},
                {".xml", "application/xml"},
                {".json", "application/json"},
                {".mp4", "video/mp4"},
                {".mp3", "audio/mpeg"},
                {".wav", "audio/wav"}
            };

            if (mimeTypes.ContainsKey(extension))
            {
                return mimeTypes[extension];
            }

            _logger.LogWarning("Extensión no reconocida: {Extension}, usando application/octet-stream", extension);
            return "application/octet-stream";
        }
    }

    public class TaskSelectVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }
        public string CourseName { get; set; }
    }
}