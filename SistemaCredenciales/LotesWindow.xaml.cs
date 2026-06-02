using Microsoft.Win32;
using SistemaCredenciales.Models;
using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SistemaCredenciales
{
    public partial class LotesWindow : Window
    {
        private string archivoSeleccionado = "";
        private TipoImportacion tipo = TipoImportacion.Ninguno;

        private enum TipoImportacion { Ninguno, Excel, Access }

        public LotesWindow()
        {
            InitializeComponent();

            cmbEstadoExport.Items.Add("Todas");
            cmbEstadoExport.Items.Add("Solo entregadas");
            cmbEstadoExport.Items.Add("Solo pendientes");
            cmbEstadoExport.SelectedIndex = 0;

            CargarEscuelasExport();
        }

        private void CargarEscuelasExport()
        {
            var db = new DatabaseService();

            cmbEscuelaExport.Items.Clear();
            cmbEscuelaExport.Items.Add("— Todas —");

            foreach (string escuela in db.ObtenerEscuelas())
                cmbEscuelaExport.Items.Add(escuela);

            cmbEscuelaExport.SelectedIndex = 0;
        }

        // ----------------------------------------------------------------
        //  IMPORTAR
        // ----------------------------------------------------------------

        private void BtnSeleccionarExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls",
                InitialDirectory = CarpetaInicial()
            };

            if (dialog.ShowDialog() != true)
                return;

            archivoSeleccionado = dialog.FileName;
            tipo = TipoImportacion.Excel;

            txtEscuela.Text =
                Path.GetFileNameWithoutExtension(archivoSeleccionado);

            try
            {
                var db = new DatabaseService();

                txtPreview.Text =
                    $"Archivo: {Path.GetFileName(archivoSeleccionado)}\n\n" +
                    db.DescribirColumnasExcel(archivoSeleccionado);

                btnImportar.IsEnabled = true;
            }
            catch (Exception ex)
            {
                txtPreview.Text =
                    "No se pudo leer el Excel:\n" + ex.Message;

                btnImportar.IsEnabled = false;
            }
        }

        private void BtnSeleccionarAccess_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Access (*.mdb)|*.mdb",
                InitialDirectory = CarpetaInicial()
            };

            if (dialog.ShowDialog() != true)
                return;

            archivoSeleccionado = dialog.FileName;
            tipo = TipoImportacion.Access;

            txtEscuela.Text =
                Path.GetFileNameWithoutExtension(archivoSeleccionado);

            try
            {
                var db = new DatabaseService();
                string tabla = db.ObtenerTablaMDB(archivoSeleccionado);

                txtPreview.Text =
                    $"Archivo: {Path.GetFileName(archivoSeleccionado)}\n\n" +
                    db.DescribirColumnasMDB(archivoSeleccionado, tabla);

                btnImportar.IsEnabled = !string.IsNullOrEmpty(tabla);
            }
            catch (Exception ex)
            {
                txtPreview.Text =
                    "No se pudo leer la base de Access:\n" + ex.Message;

                btnImportar.IsEnabled = false;
            }
        }

        private void BtnImportar_Click(object sender, RoutedEventArgs e)
        {
            string escuela = txtEscuela.Text.Trim();

            if (string.IsNullOrEmpty(escuela))
            {
                Dialogo.Show("Escribe el nombre de la escuela.");
                return;
            }

            try
            {
                var db = new DatabaseService();
                int insertadas;

                if (tipo == TipoImportacion.Excel)
                {
                    insertadas = db.ImportarExcel(archivoSeleccionado, escuela);
                }
                else if (tipo == TipoImportacion.Access)
                {
                    string tabla = db.ObtenerTablaMDB(archivoSeleccionado);
                    insertadas = db.LeerMDB(archivoSeleccionado, tabla, escuela);
                }
                else
                {
                    Dialogo.Show("Primero selecciona un archivo.");
                    return;
                }

                Dialogo.Show(
                    insertadas > 0
                        ? $"Importación terminada: {insertadas} credenciales nuevas en \"{escuela}\"."
                        : $"No se agregaron credenciales nuevas (ya existían en \"{escuela}\").");

                CargarEscuelasExport();
            }
            catch (Exception ex)
            {
                Dialogo.Show(
                    "Ocurrió un error al importar:\n\n" + ex.Message);
            }
        }

        // ----------------------------------------------------------------
        //  EXPORTAR
        // ----------------------------------------------------------------

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            string escuela = cmbEscuelaExport.SelectedItem as string ?? "— Todas —";
            string? filtroEscuela = escuela == "— Todas —" ? null : escuela;

            bool? entregada = cmbEstadoExport.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => (bool?)null
            };

            var db = new DatabaseService();

            List<CredencialImportada> datos =
                db.ObtenerCredencialesFiltradas(filtroEscuela, entregada, null, null);

            if (datos.Count == 0)
            {
                Dialogo.Show("No hay credenciales para exportar con ese filtro.");
                return;
            }

            AppConfig.Actual.AsegurarCarpetas();

            string nombreArchivo =
                $"Lote_{(filtroEscuela ?? "Todas")}_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";

            string rutaSugerida =
                Path.Combine(AppConfig.Actual.CarpetaReportes, nombreArchivo);

            var dialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = nombreArchivo,
                InitialDirectory = AppConfig.Actual.CarpetaReportes
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var export = new ExportService();
                string ruta = export.ExportarCsv(datos, dialog.FileName);

                Dialogo.Show($"Se exportaron {datos.Count} credenciales.");

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

        private string CarpetaInicial()
        {
            string carpeta = AppConfig.Actual.CarpetaBusqueda;

            return Directory.Exists(carpeta)
                ? carpeta
                : Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory);
        }
    }
}
