using SistemaCredenciales.Models;
using SistemaCredenciales.Reports;
using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SistemaCredenciales
{
    public partial class EntregasWindow : Window
    {
        private const string TodasLasEscuelas = "— Todas —";

        private List<CredencialImportada> entregas =
            new List<CredencialImportada>();

        public EntregasWindow()
        {
            InitializeComponent();

            CargarEscuelas();
        }

        private void CargarEscuelas()
        {
            var db = new DatabaseService();

            cmbEscuela.Items.Clear();
            cmbEscuela.Items.Add(TodasLasEscuelas);

            foreach (string escuela in db.ObtenerEscuelas())
                cmbEscuela.Items.Add(escuela);

            cmbEscuela.SelectedIndex = 0;
        }

        private void cmbEscuela_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            CargarEntregas();
        }

        private void CargarEntregas()
        {
            string escuela = cmbEscuela.SelectedItem as string ?? TodasLasEscuelas;
            string? filtro = escuela == TodasLasEscuelas ? null : escuela;

            var db = new DatabaseService();

            // Solo entregadas (Entregada = true).
            entregas = db.ObtenerCredencialesFiltradas(filtro, true, null, null);

            dgEntregas.ItemsSource = entregas;

            txtConteo.Text = $"{entregas.Count} entregadas";
        }

        private void BtnVerFirma_Click(object sender, RoutedEventArgs e)
        {
            AbrirFirmaSeleccionada();
        }

        private void dgEntregas_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            AbrirFirmaSeleccionada();
        }

        private void AbrirFirmaSeleccionada()
        {
            if (dgEntregas.SelectedItem == null)
            {
                Dialogo.Show("Selecciona una entrega.");
                return;
            }

            var credencial = (CredencialImportada)dgEntregas.SelectedItem;

            var db = new DatabaseService();
            string rutaFirma = db.ObtenerRutaFirma(credencial.Id);

            if (string.IsNullOrEmpty(rutaFirma) || !File.Exists(rutaFirma))
            {
                Dialogo.Show("No se encontró el archivo de firma.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = rutaFirma,
                UseShellExecute = true
            });
        }

        private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (entregas.Count == 0)
            {
                Dialogo.Show("No hay entregas para exportar.");
                return;
            }

            string escuela = cmbEscuela.SelectedItem as string ?? TodasLasEscuelas;
            string etiqueta = escuela == TodasLasEscuelas ? "Todas" : escuela;

            AppConfig.Actual.AsegurarCarpetas();

            string ruta = System.IO.Path.Combine(
                AppConfig.Actual.CarpetaReportes,
                $"Entregas_{etiqueta}_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf");

            try
            {
                var pdf = new PdfReportService();

                pdf.GenerarReporteEntregas(
                    entregas,
                    "Entregas realizadas",
                    $"Escuela: {etiqueta}",
                    ruta);

                Process.Start(new ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo generar el PDF:\n\n" + ex.Message);
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (entregas.Count == 0)
            {
                Dialogo.Show("No hay entregas para exportar.");
                return;
            }

            string escuela = cmbEscuela.SelectedItem as string ?? TodasLasEscuelas;
            string etiqueta = escuela == TodasLasEscuelas ? "Todas" : escuela;

            AppConfig.Actual.AsegurarCarpetas();

            string ruta = Path.Combine(
                AppConfig.Actual.CarpetaReportes,
                $"Entregas_{etiqueta}_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv");

            try
            {
                var export = new ExportService();
                export.ExportarCsv(entregas, ruta);

                Dialogo.Show($"Se exportaron {entregas.Count} entregas.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo exportar:\n\n" + ex.Message);
            }
        }
    }
}
