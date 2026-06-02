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
                MessageBox.Show("Escribe el nombre de la institución.");
                return;
            }

            if (!Directory.Exists(txtBusqueda.Text))
            {
                MessageBox.Show("La carpeta de búsqueda no existe.");
                return;
            }

            AppConfig cfg = AppConfig.Actual;

            cfg.NombreInstitucion = txtInstitucion.Text.Trim();
            cfg.CarpetaBusqueda = txtBusqueda.Text.Trim();
            cfg.CarpetaFirmas = txtFirmas.Text.Trim();
            cfg.CarpetaReportes = txtReportes.Text.Trim();

            cfg.Guardar();
            cfg.AsegurarCarpetas();

            MessageBox.Show("Configuración guardada.");

            Close();
        }
    }
}
