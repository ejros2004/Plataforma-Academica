using Microsoft.AspNetCore.Authorization;

namespace Control_De_Tareas.Authorization
{
    // Autorización para Administrador
    public class AdminAuthorize : AuthorizeAttribute
    {
        public AdminAuthorize() => Roles = "Administrador";
    }

    // Autorización para Profesor
    public class ProfesorAuthorize : AuthorizeAttribute
    {
        public ProfesorAuthorize() => Roles = "Profesor";
    }

    // Autorización para Estudiante
    public class EstudianteAuthorize : AuthorizeAttribute
    {
        public EstudianteAuthorize() => Roles = "Estudiante";
    }

    // Autorización para Profesor o Administrador
    public class ProfesorOAdminAuthorize : AuthorizeAttribute
    {
        public ProfesorOAdminAuthorize() => Roles = "Profesor,Administrador";
    }
}
