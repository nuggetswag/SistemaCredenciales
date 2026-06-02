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
            txtClave.Text = cfg.ClaveMaestra;

            ActualizarEstadoLogo();
        }

        private void ActualizarEstadoLogo()
        {
            txtLogoEstado.Text =
                File.Exists(AppConfig.RutaLogo)
                    ? "Logo cargado ✓"
                    : "Sin logo (se muestra el texto CUDEC)";
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

            if (string.IsNullOrWhiteSpace(txtClave.Text))
            {
                Dialogo.Show("La contraseña no puede quedar vacía.");
                return;
            }

            cfg.NombreInstitucion = txtInstitucion.Text.Trim();
            cfg.CarpetaBusqueda = txtBusqueda.Text.Trim();
            cfg.CarpetaFirmas = txtFirmas.Text.Trim();
            cfg.CarpetaReportes = txtReportes.Text.Trim();
            cfg.ClaveMaestra = txtClave.Text.Trim();

            cfg.Guardar();
            cfg.AsegurarCarpetas();

            Dialogo.Show("Configuración guardada.");

            Close();
        }
    }
}
