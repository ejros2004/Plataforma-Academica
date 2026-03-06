using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Control_De_Tareas.Configurations;

namespace Control_De_Tareas.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseUploadPath;

        public FileStorageService()
        {
            _baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "uploads"
            );
        }

        public string GetUploadPath(int courseOfferingId, int taskId)
        {
            string path = Path.Combine(
                _baseUploadPath,
                $"courseOffering_{courseOfferingId}",
                $"task_{taskId}"
            );

            return path;
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public bool ValidateFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "No se ha seleccionado ningún archivo.";
                return false;
            }

            if (file.Length > FileUploadConfig.MaxFileSize)
            {
                errorMessage = $"El archivo excede el tamaño máximo de {FileUploadConfig.MaxFileSize / 1024 / 1024}MB.";
                return false;
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!FileUploadConfig.AllowedExtensions.Contains(extension))
            {
                errorMessage = $"Extensión '{extension}' no permitida.";
                return false;
            }

            return true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, int courseOfferingId, int taskId, string uniqueFileName)
        {
            // Obtener ruta del directorio
            string directoryPath = GetUploadPath(courseOfferingId, taskId);

            // Crear directorio si no existe
            EnsureDirectoryExists(directoryPath);

            // Ruta completa del archivo
            string filePath = Path.Combine(directoryPath, uniqueFileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

       

        public async Task<string> SaveCourseDocumentAsync(IFormFile file, Guid courseOfferingId, string uniqueFileName)
        {
            var folderPath = Path.Combine(_baseUploadPath, $"course_{courseOfferingId}", "documents");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        public List<FileInfo> GetCourseDocuments(Guid courseOfferingId)
        {
            var folderPath = Path.Combine(_baseUploadPath, $"course_{courseOfferingId}", "documents");

            if (!Directory.Exists(folderPath))
            {
                return new List<FileInfo>();
            }

            var directory = new DirectoryInfo(folderPath);
            return directory.GetFiles().OrderByDescending(f => f.CreationTime).ToList();
        }

        public bool DeleteCourseDocument(Guid courseOfferingId, string fileName)
        {
            var filePath = Path.Combine(_baseUploadPath, $"course_{courseOfferingId}", "documents", fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }

            return false;
        }

        public string GetCourseDocumentPath(Guid courseOfferingId, string fileName)
        {
            return Path.Combine(_baseUploadPath, $"course_{courseOfferingId}", "documents", fileName);
        }
    }
}