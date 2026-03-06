using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class AuditLogVm
    {
        public long Id { get; set; }

        [Display(Name = "Usuario")]
        public string UserName { get; set; }

        [Display(Name = "Rol")]
        public string UserRole { get; set; }

        [Display(Name = "Acción")]
        public string Action { get; set; }

        [Display(Name = "Módulo")]
        public string Entity { get; set; }

        [Display(Name = "ID Entidad")]
        public Guid? EntityId { get; set; }

        [Display(Name = "Detalles")]
        public string Details { get; set; }

        [Display(Name = "Fecha/Hora")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Estado")]
        public bool IsSoftDeleted { get; set; }

        // Propiedades para UI según el diseño
        public string ActionDisplay => Action switch
        {
            "CREATE" => "Crear",
            "UPDATE" => "Actualizar",
            "DELETE" => "Eliminar",
            "LOGIN" => "Inicio de Sesión",
            "LOGIN_FAILED" => "Inicio de Sesión Fallido",
            "LOGOUT" => "Cierre de Sesión",
            "VIEW_AUDIT" => "Ver Auditoría",
            "SUBMIT_TASK" => "Subir Tarea",
            "GRADE_TASK" => "Calificar Tarea",
            "CREATE_COURSE" => "Crear Curso",
            "MODIFY_CONFIG" => "Modificar Configuración",
            "CREATE_ANNOUNCEMENT" => "Crear Anuncio",
            _ => Action
        };

        public string EntityDisplay => Entity switch
        {
            "Tarea" => "Tareas",
            "Usuario" => "Usuarios",
            "Curso" => "Cursos",
            "CourseOffering" => "Ofertas de Curso",
            "Auditoría" => "Auditoría",
            "Autenticación" => "Autenticación",
            "Calificaciones" => "Calificaciones",
            "Anuncios" => "Anuncios",
            "Sistema" => "Sistema",
            _ => Entity
        };

        public string EstadoBadgeClass
        {
            get
            {
                if (Action == "LOGIN_FAILED" || Action == "ERROR")
                    return "badge-danger";

                return "badge-success";
            }
        }

        public string EstadoDisplay
        {
            get
            {
                if (Action == "LOGIN_FAILED" || Action == "ERROR")
                    return "Fallido";

                return "Exitoso";
            }
        }

        public string RoleBadgeClass => UserRole switch
        {
            "Administrador" => "badge-danger",
            "Profesor" => "badge-warning",
            "Estudiante" => "badge-info",
            _ => "badge-secondary"
        };
    }
}
