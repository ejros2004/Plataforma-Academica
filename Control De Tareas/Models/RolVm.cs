using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models  
{
    public class RolVm
    {
        public Guid RoleId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}