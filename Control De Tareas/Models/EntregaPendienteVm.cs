namespace Control_De_Tareas.Models
{
    public class EntregaPendienteVm
    {
        public Guid TareaId { get; set; }
        public string TituloTarea { get; set; }
        public DateTime FechaEntrega { get; set; }
    }
}
