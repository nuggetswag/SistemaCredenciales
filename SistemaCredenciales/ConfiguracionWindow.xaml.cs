using Microsoft.Win32;
using SistemaCredenciales.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SistemaCredenciales
{
    public partial class ConfiguracionWindow : Window
    {
        public ConfiguracionWindow()
        {
            InitializeComponent();

            AppConfig cfg = AppConfig.Actual;

            txtInstitucion.Text = cfg.NombreInstitucion;
            txtBusqueda.Text = cfg.CarpetaBusqueda;
            txtFirmas.Text = cfg.CarpetaFirmas;
            txtReportes.Text = cfg.CarpetaReportes;
            txtCorreo.Text = cfg.CorreoUsuario;

            ActualizarEstadoLogo();
            ActualizarEstadoCorreo();
        }

        private void ActualizarEstadoLogo()
        {
            txtLogoEstado.Text =
                File.Exists(AppConfig.RutaLogo)
                    ? "Logo cargado ✓"
                    : "Sin logo (se muestra el texto CUDEC)";
        }

        private void ActualizarEstadoCorreo()
        {
            txtCorreoEstado.Text =
                AppConfig.Actual.CorreoConfigurado()
                    ? "Correo configurado ✓"
                    : "Sin configurar";
        }

        private void BtnElegirLogo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecciona la imagen del logo",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                File.Copy(dialog.FileName, AppConfig.RutaLogo, true);

                // Marcar con fecha actual: así el logo elegido siempre prevalece
                // y no lo reemplaza ninguna copia más antigua.
                File.SetLastWriteTime(AppConfig.RutaLogo, DateTime.Now);

                ActualizarEstadoLogo();

                // Refrescar el logo en la ventana principal al instante.
                MainWindow.Instancia?.RecargarLogo();

                Dialogo.Show("Logo actualizado.");
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo copiar el logo:\n\n" + ex.Message);
            }
        }

        private void BtnBuscarCarpeta_Click(object sender, RoutedEventArgs e)
        {
            string destino = (sender as Button)?.Tag as string ?? "";

            var dialog = new OpenFolderDialog
            {
                Title = "Selecciona una carpeta"
            };

            if (dialog.ShowDialog() != true)
                return;

            switch (destino)
            {
                case "busqueda": txtBusqueda.Text = dialog.FolderName; break;
                case "firmas": txtFirmas.Text = dialog.FolderName; break;
                case "reportes": txtReportes.Text = dialog.FolderName; break;
            }
        }

        private void BtnRestablecer_Click(object sender, RoutedEventArgs e)
        {
            txtBusqueda.Text = Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInstitucion.Text))
            {
                Dialogo.Show("Escribe el nombre de la institución.");
                return;
            }

            if (!Directory.Exists(txtBusqueda.Text))
            {
                Dialogo.Show("La carpeta de búsqueda no existe.");
                return;
            }

            AppConfig cfg = AppConfig.Actual;

            cfg.NombreInstitucion = txtInstitucion.Text.Trim();
            cfg.CarpetaBusqueda = txtBusqueda.Text.Trim();
            cfg.CarpetaFirmas = txtFirmas.Text.Trim();
            cfg.CarpetaReportes = txtReportes.Text.Trim();

            cfg.Guardar();
            cfg.AsegurarCarpetas();

            Dialogo.Show("Configuración guardada.", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Close();
        }

        // ----------------------------------------------------------------
        //  SEGURIDAD: correo de verificación
        // ----------------------------------------------------------------

        private void BtnGuardarCorreo_Click(object sender, RoutedEventArgs e)
        {
            AppConfig cfg = AppConfig.Actual;

            string nuevoCorreo = txtCorreo.Text.Trim();
            string nuevaPass = pwdApp.Password;

            if (string.IsNullOrWhiteSpace(nuevoCorreo) || !nuevoCorreo.Contains("@"))
            {
                Dialogo.Show("Escribe un correo válido.");
                return;
            }

            bool yaConfigurado = cfg.CorreoConfigurado();

            // Si ya había un correo, CAMBIARLO requiere un código al correo ACTUAL.
            if (yaConfigurado)
            {
                if (!PedirCodigo(cfg.CorreoUsuario,
                        "cambiar el correo de verificación",
                        "Te enviamos un código a tu correo ACTUAL. Escríbelo para confirmar el cambio."))
                {
                    return;
                }
            }

            // Para enviar se necesita la contraseña de aplicación.
            if (string.IsNullOrEmpty(nuevaPass))
            {
                if (!yaConfigurado || nuevoCorreo != cfg.CorreoUsuario)
                {
                    Dialogo.Show("Escribe la contraseña de aplicación del correo.");
                    return;
                }
            }

            // Guardar temporalmente para probar el envío; revertir si falla.
            string correoPrevio = cfg.CorreoUsuario;
            string passPrevia = cfg.CorreoAppPasswordProtegida;

            cfg.CorreoUsuario = nuevoCorreo;
            if (!string.IsNullOrEmpty(nuevaPass))
                cfg.EstablecerAppPassword(nuevaPass);

            try
            {
                string codigo = CodigoVerificacion.Generar();
                new CorreoService().EnviarCodigo(
                    nuevoCorreo, codigo, "prueba de configuración de correo");

                var ventana = new ClaveWindow(
                    "Enviamos un código de PRUEBA a ese correo. Escríbelo para confirmar que funciona.")
                {
                    Owner = this
                };

                if (ventana.ShowDialog() != true
                    || !CodigoVerificacion.Validar(ventana.Clave))
                {
                    throw new Exception("No se confirmó el código de prueba.");
                }
            }
            catch (Exception ex)
            {
                // Revertir: no se guarda un correo que no funciona.
                cfg.CorreoUsuario = correoPrevio;
                cfg.CorreoAppPasswordProtegida = passPrevia;

                Dialogo.Show(
                    "No se pudo verificar el correo:\n\n" + ex.Message +
                    "\n\nRevisa el correo y la contraseña de aplicación.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            cfg.Guardar();
            pwdApp.Clear();
            ActualizarEstadoCorreo();

            Dialogo.Show("Correo configurado y verificado. ✓", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCambiarPassword_Click(object sender, RoutedEventArgs e)
        {
            AppConfig cfg = AppConfig.Actual;

            if (!cfg.CorreoConfigurado())
            {
                Dialogo.Show(
                    "Primero configura y verifica el correo de verificación.");
                return;
            }

            if (!PedirCodigo(cfg.CorreoUsuario,
                    "cambiar la contraseña de Configuración",
                    "Te enviamos un código a tu correo. Escríbelo para cambiar la contraseña."))
            {
                return;
            }

            var p1 = new ClaveWindow("Escribe la NUEVA contraseña.") { Owner = this };
            if (p1.ShowDialog() != true)
                return;

            if (string.IsNullOrWhiteSpace(p1.Clave))
            {
                Dialogo.Show("La contraseña no puede quedar vacía.");
                return;
            }

            var p2 = new ClaveWindow("Repite la NUEVA contraseña.") { Owner = this };
            if (p2.ShowDialog() != true)
                return;

            if (p1.Clave != p2.Clave)
            {
                Dialogo.Show("Las contraseñas no coinciden.");
                return;
            }

            cfg.ClaveMaestra = p1.Clave;
            cfg.Guardar();

            Dialogo.Show("Contraseña cambiada. ✓", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Envía un código al correo indicado y pide al usuario escribirlo.
        /// Devuelve true si el código fue correcto.
        /// </summary>
        private bool PedirCodigo(string destino, string motivo, string mensaje)
        {
            try
            {
                string codigo = CodigoVerificacion.Generar();
                new CorreoService().EnviarCodigo(destino, codigo, motivo);
            }
            catch (Exception ex)
            {
                Dialogo.Show(
                    "No se pudo enviar el código:\n\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var ventana = new ClaveWindow(mensaje) { Owner = this };

            if (ventana.ShowDialog() != true)
                return false;

            if (!CodigoVerificacion.Validar(ventana.Clave))
            {
                Dialogo.Show("Código incorrecto o vencido.", "Acceso denegado",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
