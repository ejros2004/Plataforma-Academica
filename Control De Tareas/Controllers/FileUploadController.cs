using Microsoft.AspNetCore.Mvc;
using Control_De_Tareas.Models;
using Control_De_Tareas.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Control_De_Tareas.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly AuditService _auditService;

        public FileUploadController(IFileStorageService fileStorageService, AuditService auditService)
        {
            _fileStorageService = fileStorageService;
            _auditService = auditService;
        }

        // GET: FileUpload/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View(new FileUploadVm());
        }

        // POST: FileUpload/Upload 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(FileUploadVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (!_fileStorageService.ValidateFile(model.File, out string errorMessage))
                {
                    ModelState.AddModelError("File", errorMessage);
                    return View(model);
                }

                string extension = System.IO.Path.GetExtension(model.File.FileName);
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string uniqueFileName = $"{timestamp}_{Guid.NewGuid()}{extension}";

                string filePath = await _fileStorageService.SaveFileAsync(
                    model.File,
                    model.CourseOfferingId,
                    model.TaskId,
                    uniqueFileName
                );

                //  AUDITORÍA DE SUBIDA
                await _auditService.LogAsync("FILE_UPLOAD", "Archivo", null,
                    $"Archivo '{model.File.FileName}' ({FormatFileSize(model.File.Length)}) " +
                    $"subido a curso {model.CourseOfferingId}, tarea {model.TaskId}");

                TempData["Success"] = $"Archivo '{model.File.FileName}' subido exitosamente.";
                TempData["FileName"] = model.File.FileName;
                TempData["FileSize"] = FormatFileSize(model.File.Length);

                return RedirectToAction(nameof(Upload));
            }
            catch (Exception ex)
            {
                //AUDITORÍA DE ERROR EN SUBIDA
                await _auditService.LogAsync("FILE_UPLOAD_ERROR", "Archivo", null,
                    $"Error al subir archivo '{model.File?.FileName}': {ex.Message}");

                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }

        // GET: FileUpload/List
        [HttpGet]
        public IActionResult List(Guid? courseOfferingId, Guid? taskId)
        {
            var uploadsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "uploads"
            );

            if (!Directory.Exists(uploadsPath))
            {
                ViewBag.Files = new List<FileInfoVm>();
                return View();
            }

            var files = new List<FileInfoVm>();

            try
            {
                if (courseOfferingId.HasValue && taskId.HasValue)
                {
                    var specificPath = Path.Combine(
                        uploadsPath,
                        $"courseOffering_{courseOfferingId.Value}",
                        $"task_{taskId.Value}"
                    );

                    if (Directory.Exists(specificPath))
                    {
                        var dirInfo = new DirectoryInfo(specificPath);
                        files = dirInfo.GetFiles()
                            .Select(f => new FileInfoVm
                            {
                                FileName = f.Name,
                                FileSize = FormatFileSize(f.Length),
                                UploadDate = f.CreationTime,
                                FilePath = f.FullName,
                                CourseOfferingId = courseOfferingId.Value,
                                TaskId = taskId.Value
                            })
                            .OrderByDescending(f => f.UploadDate)
                            .ToList();
                    }
                }
                else
                {
                    var rootDir = new DirectoryInfo(uploadsPath);
                    files = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
                        .Select(f => new FileInfoVm
                        {
                            FileName = f.Name,
                            FileSize = FormatFileSize(f.Length),
                            UploadDate = f.CreationTime,
                            FilePath = f.FullName,
                            CourseOfferingId = ExtractCourseOfferingId(f.DirectoryName),
                            TaskId = ExtractTaskId(f.DirectoryName)
                        })
                        .OrderByDescending(f => f.UploadDate)
                        .Take(50)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar archivos: {ex.Message}";
                files = new List<FileInfoVm>();
            }

            ViewBag.Files = files;
            return View();
        }

        // GET: FileUpload/DownloadByName 
        [HttpGet]
        public IActionResult DownloadByName(string fileName, Guid? courseOfferingId, Guid? taskId)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    TempData["Error"] = "Nombre de archivo no válido.";
                    return RedirectToAction(nameof(List));
                }

                if (!courseOfferingId.HasValue || !taskId.HasValue)
                {
                    TempData["Error"] = "Información de ubicación no válida.";
                    return RedirectToAction(nameof(List));
                }

                string uploadsPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "uploads",
                    $"courseOffering_{courseOfferingId.Value}",
                    $"task_{taskId.Value}",
                    fileName
                );

                if (!System.IO.File.Exists(uploadsPath))
                {
                    TempData["Error"] = "El archivo no existe o fue eliminado.";
                    return RedirectToAction(nameof(List));
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(uploadsPath);
                string contentType = GetContentType(Path.GetExtension(fileName));

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al descargar: {ex.Message}";
                return RedirectToAction(nameof(List));
            }
        }

        // POST: FileUpload/DeleteFile - ACCIÓN AUDITADA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(string fileName, Guid courseOfferingId, Guid taskId)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    TempData["Error"] = "Nombre de archivo no válido.";
                    return RedirectToAction(nameof(List));
                }

                string uploadsPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "uploads",
                    $"courseOffering_{courseOfferingId}",
                    $"task_{taskId}",
                    fileName
                );

                if (!System.IO.File.Exists(uploadsPath))
                {
                    TempData["Error"] = "El archivo no existe o ya fue eliminado.";
                    return RedirectToAction(nameof(List));
                }

                // Guardar información del archivo antes de eliminar
                var fileInfo = new FileInfo(uploadsPath);
                var fileSize = FormatFileSize(fileInfo.Length);

                System.IO.File.Delete(uploadsPath);

                // AUDITORÍA DE ELIMINACIÓN
                await _auditService.LogAsync("FILE_DELETE", "Archivo", null,
                    $"Archivo '{fileName}' ({fileSize}) eliminado de curso {courseOfferingId}, tarea {taskId}");

                TempData["Success"] = $"Archivo '{fileName}' eliminado exitosamente.";
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                // AUDITORÍA DE ERROR EN ELIMINACIÓN
                await _auditService.LogAsync("FILE_DELETE_ERROR", "Archivo", null,
                    $"Error al eliminar archivo '{fileName}': {ex.Message}");

                TempData["Error"] = $"Error al eliminar archivo: {ex.Message}";
                return RedirectToAction(nameof(List));
            }
        }

  

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private Guid? ExtractCourseOfferingId(string path)
        {
            try
            {
                var parts = path.Split(Path.DirectorySeparatorChar);
                var courseOffering = parts.FirstOrDefault(p => p.StartsWith("courseOffering_"));
                if (courseOffering != null)
                {
                    var idString = courseOffering.Replace("courseOffering_", "");
                    if (Guid.TryParse(idString, out Guid id))
                        return id;
                }
            }
            catch { }
            return null;
        }

        private Guid? ExtractTaskId(string path)
        {
            try
            {
                var parts = path.Split(Path.DirectorySeparatorChar);
                var task = parts.FirstOrDefault(p => p.StartsWith("task_"));
                if (task != null)
                {
                    var idString = task.Replace("task_", "");
                    if (Guid.TryParse(idString, out Guid id))
                        return id;
                }
            }
            catch { }
            return null;
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }

    public class FileInfoVm
    {
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string FilePath { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? TaskId { get; set; }
    }
}