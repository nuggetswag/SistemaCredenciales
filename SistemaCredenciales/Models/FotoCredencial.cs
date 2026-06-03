namespace SistemaCredenciales.Models
{
    /// <summary>Una foto leída de la base (.mdb) con su matrícula y nombre.</summary>
    public class FotoCredencial
    {
        public string Matricula { get; set; } = "";

        public string Nombre { get; set; } = "";

        public byte[] Foto { get; set; } = System.Array.Empty<byte>();
    }
}
