namespace Control_De_Tareas.Models
{
    public class MenuItem
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}