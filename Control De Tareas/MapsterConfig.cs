using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Models;
using Mapster;

namespace Control_De_Tareas
{
    public static class MapsterConfig
    {
        public static void Configure()
        {
            // Configuración Roles -> RolVm
            TypeAdapterConfig<Roles, RolVm>
                .NewConfig()
                .Map(dest => dest.RoleId, src => src.RoleId)
                .Map(dest => dest.Nombre, src => src.RoleName)
                .Map(dest => dest.Descripcion, src => src.Description);

            // Configuración Module -> ModuloVm
            TypeAdapterConfig<Module, ModuloVm>
                .NewConfig()
                .Map(dest => dest.ModuleId, src => src.ModuleId)
                .Map(dest => dest.Nombre, src => src.Nombre)
                .Map(dest => dest.Metodo, src => src.Metodo)
                .Map(dest => dest.Controlador, src => src.Controller)
                .Map(dest => dest.ModuloAgrupadoId, src => src.ModuloAgrupadoId);

            // Configuración ModuleGroup -> ModuleGroupVm
            TypeAdapterConfig<ModuleGroup, ModuleGroupVm>
                .NewConfig()
                .Map(dest => dest.GroupModuleId, src => src.GroupModuleId)
                .Map(dest => dest.Descripcion, src => src.Description)
                .Map(dest => dest.Modulos, src => src.Modules);

            // Configuración RoleModules -> RolModuloVM
            TypeAdapterConfig<RoleModules, RolModuloVM>
                .NewConfig()
                .Map(dest => dest.Modulo, src => src.Module);

            // Configuración Users -> UserVm
            TypeAdapterConfig<Users, UserVm>
                .NewConfig()
                .Map(dest => dest.UserId, src => src.UserId)
                .Map(dest => dest.Nombre, src => src.UserName)  // UserName -> Nombre
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PasswordHash, src => src.PasswordHash)  // ✅ Ahora coinciden los nombres
                .Map(dest => dest.Rol, src => src.Rol);

            // Configuración para CourseOfferings -> CursoDto
            TypeAdapterConfig<CourseOfferings, CursoDto>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Codigo, src => src.Course.Code)
                .Map(dest => dest.Nombre, src => src.Course.Title)
                .Map(dest => dest.InstructorNombre, src => src.Professor.UserName)
                .Map(dest => dest.CantidadEstudiantes, src => src.Enrollments.Count(e => !e.IsSoftDeleted && e.Status == "Active"))
                .Map(dest => dest.Estado, src => src.IsActive ? "Activo" : "Inactivo");
        }
    }
}
