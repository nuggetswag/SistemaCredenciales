using SistemaCredenciales.Models;
using SistemaCredenciales.Reports;
using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SistemaCredenciales
{
    public partial class ReportesWindow : Window
    {
        private const string TodasLasEscuelas = "— Todas —";

        // Índices del ComboBox de tipo de reporte.
        private const int TipoListado = 0;
        private const int TipoEntregadas = 1;
        private const int TipoPendientes = 2;
        private const int TipoResumen = 3;
        private const int TipoPorFechas = 4;

        public ReportesWindow()
        {
            InitializeComponent();

            cmbTipo.Items.Add("Listado completo");
            cmbTipo.Items.Add("Solo entregadas");
            cmbTipo.Items.Add("Solo pendientes");
            cmbTipo.Items.Add("Resumen por escuela (totales)");
            cmbTipo.Items.Add("Entregas por rango de fechas");
            cmbTipo.SelectedIndex = 0;

            var db = new DatabaseService();

            cmbEscuela.Items.Add(TodasLasEscuelas);
            foreach (string escuela in db.ObtenerEscuelas())
                cmbEscuela.Items.Add(escuela);

            cmbEscuela.SelectedIndex = 0;

            dpDesde.SelectedDate = DateTime.Today.AddMonths(-1);
            dpHasta.SelectedDate = DateTime.Today;
        }

        private void cmbTipo_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (panelFechas == null)
                return;

            panelFechas.Visibility =
                cmbTipo.SelectedIndex == TipoPorFechas
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // El resumen siempre abarca todas las escuelas.
            cmbEscuela.IsEnabled = cmbTipo.SelectedIndex != TipoResumen;
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? ruta = GenerarReporte();

                if (ruta == null)
                    return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo generar el reporte:\n\n" + ex.Message);
            }
        }

        private string? GenerarReporte()
        {
            var db = new DatabaseService();
            var pdf = new PdfReportService();

            string escuela = cmbEscuela.SelectedItem as string ?? TodasLasEscuelas;
            string? filtroEscuela = escuela == TodasLasEscuelas ? null : escuela;
            string etiquetaEscuela = filtroEscuela ?? "Todas";

            int tipo = cmbTipo.SelectedIndex;

            AppConfig.Actual.AsegurarCarpetas();

            // ----- Resumen por escuela -----
            if (tipo == TipoResumen)
            {
                return GenerarResumen(db, pdf);
            }

            // ----- Reportes de listado -----
            bool? entregada = null;
            DateTime? desde = null;
            DateTime? hasta = null;
            string titulo;
            bool incluirFecha = false;

            if (tipo == TipoEntregadas)
            {
                entregada = true;
                incluirFecha = true;
                titulo = "Credenciales entregadas";
            }
            else if (tipo == TipoPendientes)
            {
                entregada = false;
                titulo = "Credenciales pendientes";
            }
            else if (tipo == TipoPorFechas)
            {
                entregada = true;
                incluirFecha = true;
                desde = dpDesde.SelectedDate;
                hasta = dpHasta.SelectedDate;

                if (desde == null || hasta == null)
                {
                    MessageBox.Show("Selecciona el rango de fechas.");
                    return null;
                }

                titulo = "Entregas por rango de fechas";
            }
            else
            {
                titulo = "Listado de credenciales";
            }

            List<CredencialImportada> datos =
                db.ObtenerCredencialesFiltradas(filtroEscuela, entregada, desde, hasta);

            if (datos.Count == 0)
            {
                MessageBox.Show("No hay credenciales para ese filtro.");
                return null;
            }

            bool incluirFirma = chkFirma.IsChecked == true;

            var encabezados = new List<string>
                { "Matrícula", "Nombre", "Apellidos", "Escuela", "Vigencia", "Estado" };

            if (incluirFecha)
                encabezados.Add("Fecha entrega");

            if (incluirFirma)
                encabezados.Add("Firma");

            var filas = new List<string[]>();

            foreach (CredencialImportada c in datos)
            {
                var fila = new List<string>
                {
                    c.Matricula, c.Nombre, c.Apellidos, c.Escuela, c.Vigencia,
                    c.Entregada ? "ENTREGADA" : "PENDIENTE"
                };

                if (incluirFecha)
                    fila.Add(c.FechaEntrega);

                if (incluirFirma)
                    fila.Add(string.IsNullOrEmpty(c.RutaFirma) ? "No" : "Sí");

                filas.Add(fila.ToArray());
            }

            string subtitulo =
                $"Escuela: {etiquetaEscuela}" +
                (desde != null
                    ? $"   |   {desde:dd/MM/yyyy} a {hasta:dd/MM/yyyy}"
                    : "");

            string ruta = Path.Combine(
                AppConfig.Actual.CarpetaReportes,
                $"Reporte_{Tipo(tipo)}_{etiquetaEscuela}_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf");

            return pdf.GenerarReporte(
                titulo, subtitulo, encabezados.ToArray(), filas, ruta);
        }

        private string? GenerarResumen(DatabaseService db, PdfReportService pdf)
        {
            List<CredencialImportada> todas =
                db.ObtenerCredencialesFiltradas(null, null, null, null);

            if (todas.Count == 0)
            {
                MessageBox.Show("No hay credenciales para resumir.");
                return null;
            }

            var encabezados = new[]
                { "Escuela", "Total", "Entregadas", "Pendientes", "% Avance" };

            var filas = new List<string[]>();

            foreach (var grupo in todas
                .GroupBy(c => c.Escuela)
                .OrderBy(g => g.Key))
            {
                int total = grupo.Count();
                int entregadas = grupo.Count(c => c.Entregada);
                int pendientes = total - entregadas;
                double avance = total > 0 ? (double)entregadas / total * 100 : 0;

                filas.Add(new[]
                {
                    grupo.Key,
                    total.ToString(),
                    entregadas.ToString(),
                    pendientes.ToString(),
                    $"{avance:0}%"
                });
            }

            // Fila de totales generales.
            int totalGeneral = todas.Count;
            int entregadasGeneral = todas.Count(c => c.Entregada);
            int pendientesGeneral = totalGeneral - entregadasGeneral;
            double avanceGeneral =
                totalGeneral > 0
                    ? (double)entregadasGeneral / totalGeneral * 100
                    : 0;

            filas.Add(new[]
            {
                "TOTAL GENERAL",
                totalGeneral.ToString(),
                entregadasGeneral.ToString(),
                pendientesGeneral.ToString(),
                $"{avanceGeneral:0}%"
            });

            string ruta = Path.Combine(
                AppConfig.Actual.CarpetaReportes,
                $"Resumen_Escuelas_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf");

            return pdf.GenerarReporte(
                "Resumen por escuela",
                "Avance de entrega de credenciales",
                encabezados, filas, ruta);
        }

        private string Tipo(int tipo)
        {
            return tipo switch
            {
                TipoEntregadas => "Entregadas",
                TipoPendientes => "Pendientes",
                TipoPorFechas => "PorFechas",
                _ => "Listado"
            };
        }
    }
}
