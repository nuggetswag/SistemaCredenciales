using SistemaCredenciales.Models;
using SistemaCredenciales.Reports;
using SistemaCredenciales.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace SistemaCredenciales
{
    public partial class MainWindow : Window
    {
        private List<CredencialImportada> todasLasCredenciales =
            new List<CredencialImportada>();
        public static MainWindow Instancia;


        private void ActualizarDashboard()
        {
            int total =
                todasLasCredenciales.Count;

            int entregadas =
                todasLasCredenciales.Count(x =>
                    x.Entregada);

            int pendientes =
                total - entregadas;

            double avance =
                total > 0
                ? (double)entregadas / total * 100
                : 0;

            txtTotal.Text =
                total.ToString();

            txtEntregadas.Text =
                entregadas.ToString();

            txtPendientes.Text =
                pendientes.ToString();

            txtAvance.Text =
                $"{avance:0}%";
        }


        private void BtnTotal_Click(object sender,
    RoutedEventArgs e)
        {
            dgEscuelas.ItemsSource =
                todasLasCredenciales;
        }

        private void BtnEntregadas_Click(object sender,
            RoutedEventArgs e)
        {
            dgEscuelas.ItemsSource =
                todasLasCredenciales
                .Where(x => x.Entregada)
                .ToList();
        }

        private void BtnPendientes_Click(object sender,
            RoutedEventArgs e)
        {
            dgEscuelas.ItemsSource =
                todasLasCredenciales
                .Where(x => !x.Entregada)
                .ToList();
        }

        public MainWindow()
        {
            InitializeComponent();
            Instancia = this;

            txtBuscar.Text =
             "Buscar matrícula o nombre...";

            DatabaseService db = new DatabaseService();

            todasLasCredenciales =
                db.ObtenerCredenciales();

            dgEscuelas.ItemsSource =
                todasLasCredenciales;
            ActualizarDashboard();


           
        }


        public void RecargarCredenciales()
        {
            DatabaseService db =
                new DatabaseService();

            todasLasCredenciales =
                db.ObtenerCredenciales();

            dgEscuelas.ItemsSource =
                todasLasCredenciales;

            ActualizarDashboard();
        }

        private void txtBuscar_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            if (dgEscuelas == null)
                return;

            string texto = txtBuscar.Text.ToLower();

            var filtradas = todasLasCredenciales.Where(x =>
                x.Matricula.ToLower().Contains(texto)
                || x.Nombre.ToLower().Contains(texto)
                || x.Apellidos.ToLower().Contains(texto)
            ).ToList();

            dgEscuelas.ItemsSource = filtradas;
        }

        private void txtBuscar_GotFocus(object sender,
    RoutedEventArgs e)
        {
            if (txtBuscar.Text ==
                "Buscar matrícula o nombre...")
            {
                txtBuscar.Text = "";
            }
        }

        private void txtBuscar_LostFocus(object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                txtBuscar.Text))
            {
                txtBuscar.Text =
                    "Buscar matrícula o nombre...";
            }
        }


        private void BtnEntregar_Click(object sender,
    RoutedEventArgs e)
        {
            if (dgEscuelas.SelectedItem == null)
            {
                MessageBox.Show(
                    "Selecciona una credencial.");

                return;
            }

            CredencialImportada credencial =
                (CredencialImportada)dgEscuelas.SelectedItem;

            if (credencial.Entregada)
            {
                MessageBox.Show(
                    "La credencial ya está entregada.");

                return;
            }

            DatabaseService db =
                new DatabaseService();

            FirmaWindow firma =
    new FirmaWindow(
        credencial.Matricula,
        credencial.Id);

            firma.ShowDialog();

            if (!firma.FirmaGuardada)
            {
                MessageBox.Show(
                    "Entrega cancelada.");

                return;
            }

            db.CambiarEstadoEntrega(
                credencial.Id,
                true);

            MessageBox.Show(
                "Credencial entregada.");

            todasLasCredenciales =
                db.ObtenerCredenciales();

            dgEscuelas.ItemsSource =
                todasLasCredenciales;
            ActualizarDashboard();
        }


        private void BtnCancelarEntrega_Click(object sender,
    RoutedEventArgs e)
        {
            if (dgEscuelas.SelectedItem == null)
            {
                MessageBox.Show(
                    "Selecciona una credencial.");

                return;
            }

            CredencialImportada credencial =
                (CredencialImportada)dgEscuelas.SelectedItem;

            if (!credencial.Entregada)
            {
                MessageBox.Show(
                    "La credencial ya está pendiente.");

                return;
            }

            MessageBoxResult resultado =
                MessageBox.Show(
                    "¿Seguro que deseas cancelar esta entrega?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes)
                return;

            DatabaseService db =
                new DatabaseService();

            string rutaFirma =
                db.ObtenerRutaFirma(credencial.Id);

            if (System.IO.File.Exists(rutaFirma))
            {
                System.IO.File.Delete(rutaFirma);
            }

            db.LimpiarRutaFirma(credencial.Id);

            db.CambiarEstadoEntrega(
                credencial.Id,
                false);

            MessageBox.Show(
                "Entrega cancelada.");

            todasLasCredenciales =
                db.ObtenerCredenciales();

            dgEscuelas.ItemsSource =
                todasLasCredenciales;
            ActualizarDashboard();
        }


        private void dgEscuelas_MouseDoubleClick(object sender,
    System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgEscuelas.SelectedItem == null)
                return;

            CredencialImportada credencial =
                (CredencialImportada)dgEscuelas.SelectedItem;

            DatabaseService db =
                new DatabaseService();

            string rutaFirma =
                db.ObtenerRutaFirma(credencial.Id);

            if (string.IsNullOrEmpty(rutaFirma))
            {
                MessageBox.Show(
                    "Esta credencial no tiene firma.");

                return;
            }

            if (!System.IO.File.Exists(rutaFirma))
            {
                MessageBox.Show(
                    "No se encontró el archivo de firma.");

                return;
            }

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = rutaFirma,
                    UseShellExecute = true
                });
        }


        private void BtnReportes_Click(object sender,
    RoutedEventArgs e)
        {
            PdfReportService pdf =
                new PdfReportService();

            pdf.GenerarReporte(
                todasLasCredenciales);

            MessageBox.Show(
                "Reporte generado correctamente.");

            Process.Start(new ProcessStartInfo
            {
                FileName =
    Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "ReporteCredenciales.pdf"),

                UseShellExecute = true
            });
        }



        private void BtnEscuelas_Click(object sender,
    RoutedEventArgs e)
        {
            EscuelasWindow ventana =
                new EscuelasWindow();

            ventana.ShowDialog();
        }















    }
}