using System;
using System.IO;
using System.IO.Compression;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Crea y restaura respaldos (.zip) con la base de datos, las firmas y la
    /// configuración. Protege la información crítica del programa.
    /// </summary>
    public class RespaldoService
    {
        private static string BaseDir =>
            AppDomain.CurrentDomain.BaseDirectory;

        private static string RutaDb =>
            Path.Combine(BaseDir, "SistemaCredenciales.db");

        private static string RutaConfig =>
            Path.Combine(BaseDir, "config.json");

        private const string NombreDb = "SistemaCredenciales.db";
        private const string CarpetaFirmasZip = "Firmas/";

        /// <summary>Crea un .zip con la base, las firmas y el config.</summary>
        public string CrearRespaldo(string rutaZip)
        {
            if (File.Exists(rutaZip))
                File.Delete(rutaZip);

            using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
            {
                if (File.Exists(RutaDb))
                    zip.CreateEntryFromFile(RutaDb, NombreDb);

                if (File.Exists(RutaConfig))
                    zip.CreateEntryFromFile(RutaConfig, "config.json");

                string firmas = AppConfig.Actual.CarpetaFirmas;

                if (Directory.Exists(firmas))
                {
                    foreach (string archivo in Directory.GetFiles(firmas))
                    {
                        zip.CreateEntryFromFile(
                            archivo,
                            CarpetaFirmasZip + Path.GetFileName(archivo));
                    }
                }
            }

            return rutaZip;
        }

        /// <summary>
        /// Restaura un respaldo: reemplaza la base de datos y las firmas por las
        /// del .zip. (No toca la configuración del equipo actual.)
        /// </summary>
        public void RestaurarRespaldo(string rutaZip)
        {
            using (var zip = ZipFile.OpenRead(rutaZip))
            {
                ZipArchiveEntry? dbEntry = zip.GetEntry(NombreDb);

                if (dbEntry == null)
                {
                    throw new Exception(
                        "El archivo no parece un respaldo válido " +
                        "(no contiene la base de datos).");
                }

                // Reemplazar la base de datos.
                dbEntry.ExtractToFile(RutaDb, overwrite: true);

                // Reemplazar las firmas.
                string firmas = AppConfig.Actual.CarpetaFirmas;
                Directory.CreateDirectory(firmas);

                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (entry.FullName.StartsWith(CarpetaFirmasZip)
                        && !string.IsNullOrEmpty(entry.Name))
                    {
                        string destino =
                            Path.Combine(firmas, entry.Name);

                        entry.ExtractToFile(destino, overwrite: true);
                    }
                }
            }
        }
    }
}
