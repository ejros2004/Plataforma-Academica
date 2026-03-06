using Microsoft.AspNetCore.Mvc;

namespace Control_De_Tareas.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/403")]
        public IActionResult Error403() => View();

        [Route("Error/404")]
        public IActionResult Error404() => View();
    }
}
