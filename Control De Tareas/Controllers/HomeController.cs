using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Control_De_Tareas.Controllers
{
    public class HomeController : Controller
    {
        private readonly ContextDB _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ContextDB context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Verificar si hay sesión de usuario
            var encoded = HttpContext.Session.GetString("UserSession");

            if (!string.IsNullOrEmpty(encoded))
            {
                try
                {
                    // Deserializar el usuario de la sesión
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    var user = JsonConvert.DeserializeObject<UserVm>(json);

                    // Redirigir según el rol
                    if (user?.Rol?.Nombre != null)
                    {
                        return user.Rol.Nombre switch
                        {
                            "Administrador" => RedirectToAction("Admin", "Dashboard"),
                            "Profesor" => RedirectToAction("Profesor", "Dashboard"),
                            "Estudiante" => RedirectToAction("Estudiante", "Dashboard"),
                            _ => View() // Página principal para otros roles o sin rol
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al deserializar sesión de usuario");
                    HttpContext.Session.Remove("UserSession");
                }
            }

            // Mostrar página principal pública
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            // Si ya está autenticado, redirigir al dashboard según rol
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Redirigir al AccountController donde está el registro real
            return RedirectToAction("Register", "Account");
        }

        // Password Recovery - redirigir a Account
        public IActionResult PasswordRecovery()
        {
            // Si no tienes esta funcionalidad, puedes comentarla o eliminar
            return View(); // o return RedirectToAction("PasswordRecovery", "Account");
        }

        // Code Verification - redirigir a Account
        public IActionResult VerifyCode()
        {
            // Si no tienes esta funcionalidad, puedes comentarla o eliminar
            return View(); // o return RedirectToAction("VerifyCode", "Account");
        }

        // Password Change - redirigir a Account
        public IActionResult ChangePassword()
        {
            return RedirectToAction("ChangePassword", "Account");
        }
    }
}