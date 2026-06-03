using Microsoft.Data.Sqlite;
using SistemaCredenciales.Models;
using System;
using System.Collections.Generic;
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
        /// Exporta las fotos a un .zip, cada una nombrada por su matrícula
        /// (ej. 125055.jpg). Devuelve cuántas se guardaron.
        /// </summary>
        public int ExportarFotos(List<FotoCredencial> fotos, string rutaZip)
        {
            if (File.Exists(rutaZip))
                File.Delete(rutaZip);

            int guardadas = 0;
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
            {
                foreach (FotoCredencial f in fotos)
                {
                    if (f.Foto == null || f.Foto.Length == 0)
                        continue;

                    string baseNombre = SanitizarNombre(
                        string.IsNullOrWhiteSpace(f.Matricula)
                            ? "sin_matricula"
                            : f.Matricula);

                    string nombre = baseNombre + ".jpg";
                    int n = 2;
                    while (usados.Contains(nombre))
                    {
                        nombre = $"{baseNombre}_{n}.jpg";
                        n++;
                    }
                    usados.Add(nombre);

                    ZipArchiveEntry entry = zip.CreateEntry(nombre);
                    using (Stream s = entry.Open())
                        s.Write(f.Foto, 0, f.Foto.Length);

                    guardadas++;
                }
            }

            return guardadas;
        }

        private static string SanitizarNombre(string nombre)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');

            return nombre.Trim();
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
