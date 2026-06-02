using SistemaCredenciales.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Exporta credenciales a CSV compatible con Excel (UTF-8 con BOM y ';'
    /// como separador, que es lo que Excel en español espera por defecto).
    /// </summary>
    public class ExportService
    {
        public string ExportarCsv(
            List<CredencialImportada> credenciales,
            string rutaSalida)
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                "Matricula;Nombre;Apellidos;Vigencia;Escuela;Estado;FechaEntrega");

            foreach (CredencialImportada c in credenciales)
            {
                sb.AppendLine(string.Join(";",
                    Campo(c.Matricula),
                    Campo(c.Nombre),
                    Campo(c.Apellidos),
                    Campo(c.Vigencia),
                    Campo(c.Escuela),
                    c.Entregada ? "ENTREGADA" : "PENDIENTE",
                    Campo(c.FechaEntrega)));
            }

            // UTF-8 con BOM para que Excel respete los acentos.
            File.WriteAllText(rutaSalida, sb.ToString(),
                new UTF8Encoding(true));

            return rutaSalida;
        }

        /// <summary>Escapa un campo para CSV (comillas si contiene ; " o saltos).</summary>
        private string Campo(string valor)
        {
            valor = valor ?? "";

            if (valor.Contains(";") || valor.Contains("\"")
                || valor.Contains("\n") || valor.Contains("\r"))
            {
                valor = "\"" + valor.Replace("\"", "\"\"") + "\"";
            }

            return valor;
        }
    }
}
