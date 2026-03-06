using Control_De_Tareas.Models;

namespace Control_De_Tareas.Services
{
    /// <summary>
    /// Servicio estático encargado de proporcionar la estructura del menú lateral
    /// de la aplicación según los roles permitidos.
    /// Cada elemento indica qué controlador y acción debe ejecutarse.
    /// </summary>
    public class MenuServices
    {
        /// <summary>
        /// Lista estática que contiene los elementos del menú disponibles en la aplicación.
        /// Cada ítem especifica:
        /// - Texto visible (Label)
        /// - Ícono (Icon)
        /// - Roles permitidos
        /// - Controlador y acción correspondientes
        /// </summary>
        public static List<MenuItem> Items = new()
        {
            new MenuItem
            {
                Id = "dashboard",
                Label = "Dashboard",
                Icon = "layout-dashboard",
                Roles = ["admin","profesor","estudiante"],
                Controller = "Dashboard",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "courses",
                Label = "Cursos",
                Icon = "book-open",
                Roles = ["admin","profesor","estudiante"],
                Controller = "Cursos",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "tasks",
                Label = "Tareas",
                Icon = "clipboard-list",
                Roles = ["admin","profesor"],
                Controller = "Tareas",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "grade",
                Label = "Calificar",
                Icon = "graduation-cap",
                Roles = ["profesor"],
                Controller = "Grade",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "announcements",
                Label = "Anuncios",
                Icon = "megaphone",
                Roles = ["profesor"],
                Controller = "Anuncios",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "submissions",
                Label = "Mis Entregas",
                Icon = "upload",
                Roles = ["estudiante"],
                Controller = "Entregas",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "grades",
                Label = "Calificaciones",
                Icon = "bar-chart-3",
                Roles = ["estudiante"],
                Controller = "Grades",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "users",
                Label = "Usuarios",
                Icon = "users",
                Roles = ["admin"],
                Controller = "Usuarios",
                Action = "Index"
            },

            new MenuItem
            {
                Id = "audit",
                Label = "Auditoría",
                Icon = "file-text",
                Roles = ["admin"],
                Controller = "Audit",
                Action = "Index"
            }
        };
    }
}
