namespace SistemaCredenciales.Models
{
    public class CredencialImportada
    {
        public int Id { get; set; }

        public string Matricula { get; set; } = "";

        public string Nombre { get; set; } = "";

        public string Apellidos { get; set; } = "";

        public string Vigencia { get; set; } = "";

        public string Escuela { get; set; } = "";

        public string Area { get; set; } = "";

        public bool Entregada { get; set; }

        public string FechaEntrega { get; set; } = "";

        public string RutaFirma { get; set; } = "";
    }
}