using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Control_De_Tareas.Services
{
    public interface IFileStorageService
    {
        // Métodos existentes
        string GetUploadPath(int courseOfferingId, int taskId);
        void EnsureDirectoryExists(string path);
        bool ValidateFile(IFormFile file, out string errorMessage);
        Task<string> SaveFileAsync(IFormFile file, int courseOfferingId, int taskId, string uniqueFileName);

        // Métodos nuevos para documentos de curso
        Task<string> SaveCourseDocumentAsync(IFormFile file, Guid courseOfferingId, string uniqueFileName);
        List<FileInfo> GetCourseDocuments(Guid courseOfferingId);
        bool DeleteCourseDocument(Guid courseOfferingId, string fileName);
        string GetCourseDocumentPath(Guid courseOfferingId, string fileName);
    }
}