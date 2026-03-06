namespace Control_De_Tareas.Configurations
{
    public static class FileUploadConfig
    {
        public static string UploadBaseDirectory => "uploads";

        public static long MaxFileSize => 10 * 1024 * 1024; // 10MB

        public static string[] AllowedExtensions => new[]
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt"
        };
    }
}