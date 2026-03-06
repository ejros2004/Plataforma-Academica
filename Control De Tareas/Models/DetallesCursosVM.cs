namespace Control_De_Tareas.Models
{
    public class DetallesCursosVM
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public int TotalDocumentos { get; set; }
        public int TotalEnlaces { get; set; }
        public int TotalTareas { get; set; }
        public int TotalEvaluaciones { get; set; }
        public int TotalAnuncios { get; set; }

        public bool EsAdmin { get; set; }
        public bool EsProfesor { get; set; }
        public bool EsEstudiante { get; set; }
    }
}