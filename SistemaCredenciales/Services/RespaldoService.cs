using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.IO.Compression;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Crea y restaura respaldos (.zip) con la base de datos, las firmas y la
    /// configuración. Usa el respaldo interno de SQLite para copiar la base
    /// aunque el programa la tenga abierta.
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

            // Copia consistente de la base con el API de respaldo de SQLite
            // (funciona aunque la base esté abierta por el programa).
            string tempDb =
                Path.Combine(
                    Path.GetTempPath(),
                    "cred_backup_" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                using (var origen = new SqliteConnection($"Data Source={RutaDb}"))
                using (var destino = new SqliteConnection($"Data Source={tempDb}"))
                {
                    origen.Open();
                    destino.Open();
                    origen.BackupDatabase(destino);
                }

                // Libera los archivos que SQLite pudiera tener en el pool.
                SqliteConnection.ClearAllPools();

                using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
                {
                    if (File.Exists(tempDb))
                        zip.CreateEntryFromFile(tempDb, NombreDb);

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
            }
            finally
            {
                try { if (File.Exists(tempDb)) File.Delete(tempDb); }
                catch { /* archivo temporal; si no se borra, no pasa nada */ }
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

                // Liberar la base actual para poder reemplazarla.
                SqliteConnection.ClearAllPools();

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
