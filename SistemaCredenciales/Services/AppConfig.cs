using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Configuración portable de la aplicación. Se guarda en un "config.json"
    /// junto al ejecutable, de modo que el programa pueda moverse de un PC a otro
    /// sin perder sus rutas. Acceso global a través de <see cref="Actual"/>.
    /// </summary>
    public class AppConfig
    {
        public string CarpetaBusqueda { get; set; } = "";

        public string CarpetaFirmas { get; set; } = "";

        public string CarpetaReportes { get; set; } = "";

        public string NombreInstitucion { get; set; } = "";

        /// <summary>Contraseña para acciones sensibles (marcado masivo, login).</summary>
        public string ClaveMaestra { get; set; } = "";

        /// <summary>Correo (Gmail) que envía y recibe los códigos de verificación.</summary>
        public string CorreoUsuario { get; set; } = "";

        /// <summary>Contraseña de aplicación del correo, cifrada (DPAPI).</summary>
        public string CorreoAppPasswordProtegida { get; set; } = "";

        public string SmtpHost { get; set; } = "smtp.gmail.com";

        public int SmtpPuerto { get; set; } = 587;

        public bool SmtpSsl { get; set; } = true;

        private static AppConfig? _actual;

        /// <summary>Ruta del logo de la institución (junto al ejecutable).</summary>
        public static string RutaLogo =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "logo.png");

        private static readonly string RutaConfig =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.json");

        private static string CarpetaBase =>
            AppDomain.CurrentDomain.BaseDirectory;

        public static AppConfig Actual
        {
            get
            {
                if (_actual == null)
                    _actual = Cargar();

                return _actual;
            }
        }

        private static AppConfig Predeterminada()
        {
            return new AppConfig
            {
                CarpetaBusqueda =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.DesktopDirectory),

                CarpetaFirmas =
                    Path.Combine(CarpetaBase, "Firmas"),

                CarpetaReportes =
                    Path.Combine(CarpetaBase, "Reportes"),

                NombreInstitucion = "CUDEC",

                ClaveMaestra = "cudec2026"
            };
        }

        private static AppConfig Cargar()
        {
            try
            {
                if (File.Exists(RutaConfig))
                {
                    string json = File.ReadAllText(RutaConfig);

                    AppConfig? cfg =
                        JsonSerializer.Deserialize<AppConfig>(json);

                    if (cfg != null)
                    {
                        cfg.Normalizar();
                        return cfg;
                    }
                }
            }
            catch
            {
                // Si el archivo está corrupto o ilegible, se usa la configuración
                // predeterminada en lugar de tronar al abrir el programa.
            }

            AppConfig predeterminada = Predeterminada();
            predeterminada.Guardar();
            return predeterminada;
        }

        private void Normalizar()
        {
            AppConfig def = Predeterminada();

            if (string.IsNullOrWhiteSpace(CarpetaBusqueda))
                CarpetaBusqueda = def.CarpetaBusqueda;

            if (string.IsNullOrWhiteSpace(CarpetaFirmas))
                CarpetaFirmas = def.CarpetaFirmas;

            if (string.IsNullOrWhiteSpace(CarpetaReportes))
                CarpetaReportes = def.CarpetaReportes;

            if (string.IsNullOrWhiteSpace(NombreInstitucion))
                NombreInstitucion = def.NombreInstitucion;

            if (string.IsNullOrWhiteSpace(ClaveMaestra))
                ClaveMaestra = def.ClaveMaestra;

            if (string.IsNullOrWhiteSpace(SmtpHost))
                SmtpHost = "smtp.gmail.com";

            if (SmtpPuerto <= 0)
                SmtpPuerto = 587;
        }

        public void Guardar()
        {
            try
            {
                string json =
                    JsonSerializer.Serialize(
                        this,
                        new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(RutaConfig, json);
            }
            catch
            {
                // No bloquear la app si no se puede escribir el archivo.
            }
        }

        public void AsegurarCarpetas()
        {
            Directory.CreateDirectory(CarpetaFirmas);
            Directory.CreateDirectory(CarpetaReportes);
        }

        // ---- Correo / contraseña de aplicación (cifrada) ----

        public string ObtenerAppPassword()
            => ProteccionService.Desproteger(CorreoAppPasswordProtegida);

        public void EstablecerAppPassword(string textoPlano)
            => CorreoAppPasswordProtegida = ProteccionService.Proteger(textoPlano);

        /// <summary>True si hay correo y contraseña de aplicación configurados.</summary>
        public bool CorreoConfigurado()
            => !string.IsNullOrWhiteSpace(CorreoUsuario)
               && !string.IsNullOrWhiteSpace(CorreoAppPasswordProtegida);

        /// <summary>
        /// Carpetas donde se buscarán archivos de escuelas (.mdb / .xlsx).
        /// Si la carpeta de búsqueda sigue siendo el escritorio predeterminado,
        /// se incluyen todas las variantes de escritorio (clásico y OneDrive)
        /// para que funcione en cualquier PC. Si el usuario eligió una carpeta
        /// específica, solo se usa esa.
        /// </summary>
        public IEnumerable<string> CarpetasDeBusqueda()
        {
            AppConfig def = Predeterminada();

            bool esEscritorioPorDefecto =
                string.Equals(
                    (CarpetaBusqueda ?? "").TrimEnd('\\', '/'),
                    (def.CarpetaBusqueda ?? "").TrimEnd('\\', '/'),
                    StringComparison.OrdinalIgnoreCase);

            var carpetas = new List<string>();

            if (esEscritorioPorDefecto)
            {
                foreach (string escritorio in Escritorios())
                {
                    if (Directory.Exists(escritorio)
                        && !carpetas.Contains(escritorio, StringComparer.OrdinalIgnoreCase))
                    {
                        carpetas.Add(escritorio);
                    }
                }
            }
            else if (Directory.Exists(CarpetaBusqueda))
            {
                carpetas.Add(CarpetaBusqueda);
            }

            return carpetas;
        }

        private static IEnumerable<string> Escritorios()
        {
            yield return Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);

            string userProfile =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);

            if (!string.IsNullOrEmpty(userProfile))
                yield return Path.Combine(userProfile, "Desktop");

            foreach (string variable in new[]
                { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
            {
                string? ruta =
                    Environment.GetEnvironmentVariable(variable);

                if (!string.IsNullOrEmpty(ruta))
                    yield return Path.Combine(ruta, "Desktop");
            }
        }
    }
}
