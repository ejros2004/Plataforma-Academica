using Microsoft.AspNetCore.Mvc;
using Control_De_Tareas.Services;

namespace Control_De_Tareas.Controllers
{
    public class TestController : Controller
    {
        private readonly FileStorageService _fileStorageService;

        public TestController(FileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        // GET: /Test/TestStructure
        public IActionResult TestStructure()
        {
            try
            {
                // Probar con valores de ejemplo
                int courseOfferingId = 1;
                int taskId = 5;

                // Obtener y crear la ruta
                string path = _fileStorageService.GetUploadPath(courseOfferingId, taskId);
                _fileStorageService.EnsureDirectoryExists(path);

                // Verificar que existe
                bool exists = Directory.Exists(path);

                return Ok(new
                {
                    message = "Estructura creada exitosamente",
                    path = path,
                    exists = exists
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }
    }
}