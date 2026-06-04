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
    public partial class CredencialGenerarWindow : Window
    {
        private const string TodasLasEscuelas = "— Todas —";

        private readonly DatabaseService db = new DatabaseService();
        private readonly DisenoService disenoService = new DisenoService();
        private readonly CredencialRenderService render = new CredencialRenderService();

        private List<CredencialImportada> personas = new List<CredencialImportada>();

        public CredencialGenerarWindow()
        {
            InitializeComponent();

            CargarEscuelas();
            CargarCategorias();
            CargarPersonas();
        }

        private void CargarCategorias()
        {
            string? seleccion = cmbCategoria.SelectedItem as string;

            cmbCategoria.Items.Clear();
            cmbCategoria.Items.Add("Todas");

            cmbAsignar.Items.Clear();
            foreach (string t in DisenoService.Tipos) cmbAsignar.Items.Add(t);

            foreach (string c in db.ObtenerCategorias())
            {
                cmbCategoria.Items.Add(c);
                if (!cmbAsignar.Items.Contains(c)) cmbAsignar.Items.Add(c);
            }

            cmbCategoria.SelectedItem = cmbCategoria.Items.Contains(seleccion)
                ? seleccion : "Todas";
            if (cmbCategoria.SelectedItem == null) cmbCategoria.SelectedIndex = 0;
        }

        private void CargarEscuelas()
        {
            cmbEscuela.Items.Clear();
            cmbEscuela.Items.Add(TodasLasEscuelas);
            foreach (string esc in db.ObtenerEscuelas())
                cmbEscuela.Items.Add(esc);
            cmbEscuela.SelectedIndex = 0;
        }

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) CargarPersonas();
        }

        private void CargarPersonas()
        {
            string escuela = cmbEscuela.SelectedItem as string ?? TodasLasEscuelas;
            string? filtro = escuela == TodasLasEscuelas ? null : escuela;
            string categoria = cmbCategoria.SelectedItem as string ?? "Todas";

            personas = db.ObtenerCredencialesFiltradas(filtro, null, null, null);

            if (categoria != "Todas")
                personas = personas.Where(p => (p.Categoria ?? "") == categoria).ToList();

            dgPersonas.ItemsSource = personas;
            ActualizarConteo();
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ActualizarConteo();

        private void ActualizarConteo()
        {
            int sel = dgPersonas.SelectedItems.Count;
            txtConteo.Text = sel > 0
                ? $"{sel} seleccionadas"
                : $"{personas.Count} en total";
        }

        private List<CredencialImportada> Seleccionadas()
        {
            if (dgPersonas.SelectedItems.Count > 0)
                return dgPersonas.SelectedItems.Cast<CredencialImportada>().ToList();
            return personas;
        }

        private void BtnAplicarCat_Click(object sender, RoutedEventArgs e)
        {
            string cat = cmbAsignar.Text.Trim();
            if (string.IsNullOrEmpty(cat))
            {
                Dialogo.Show("Escribe o elige una categoría para asignar.");
                return;
            }

            var sel = dgPersonas.SelectedItems.Cast<CredencialImportada>().ToList();
            if (sel.Count == 0)
            {
                Dialogo.Show("Selecciona primero las personas a las que cambiar la categoría.");
                return;
            }

            foreach (CredencialImportada p in sel)
            {
                db.ActualizarCategoria(p.Id, cat);
                p.Categoria = cat;
            }

            new BitacoraService().Registrar("Cambiar categoría",
                $"{sel.Count} → {cat}");

            CargarCategorias();
            CargarPersonas();

            Dialogo.Show($"Categoría \"{cat}\" asignada a {sel.Count} persona(s). ✓",
                "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private (byte[] frente, byte[]? reverso) RenderPersona(CredencialImportada p)
        {
            string tipo = string.IsNullOrWhiteSpace(p.Categoria) ? "Alumno" : p.Categoria;
            DisenoCredencial dFrente = disenoService.Obtener(tipo, "frente");
            DisenoCredencial dReverso = disenoService.Obtener(tipo, "reverso");

            byte[]? foto = db.ObtenerFotoCredencial(p);
            byte[] frente = render.RenderLado(p, dFrente, foto);

            byte[]? reverso = null;
            bool hayReverso = !string.IsNullOrWhiteSpace(dReverso.PlantillaRuta)
                              || dReverso.Campos.Count > 0;
            if (hayReverso)
                reverso = render.RenderLado(p, dReverso, null);

            return (frente, reverso);
        }

        private void BtnDisenador_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new CredencialDisenoWindow { Owner = this }.ShowDialog();
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo abrir el diseñador:\n\n" + ex,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTodos_Click(object sender, RoutedEventArgs e)
            => dgPersonas.SelectAll();

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            var lista = Seleccionadas();
            if (lista.Count == 0)
            {
                Dialogo.Show("No hay personas para previsualizar.");
                return;
            }

            try
            {
                byte[] frente = RenderPersona(lista[0]).frente;
                string ruta = Path.Combine(
                    Path.GetTempPath(),
                    $"preview_credencial_{DateTime.Now:HHmmss}.png");
                File.WriteAllBytes(ruta, frente);
                Process.Start(new ProcessStartInfo { FileName = ruta, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo generar la vista previa:\n\n" + ex.Message);
            }
        }

        private void BtnIndividual_Click(object sender, RoutedEventArgs e)
        {
            var lista = Seleccionadas();
            if (lista.Count == 0) { Dialogo.Show("Selecciona al menos una persona."); return; }

            try
            {
                var creds = new List<(byte[] frente, byte[]? reverso)>();
                foreach (CredencialImportada p in lista)
                    creds.Add(RenderPersona(p));

                string ruta = RutaSalida("Credenciales");
                new PdfReportService().GenerarCredencialesIndividual(creds, ruta);

                new BitacoraService().Registrar("Generar credenciales (individual)",
                    $"{lista.Count} credenciales");

                Abrir(ruta);
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudieron generar las credenciales:\n\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLote_Click(object sender, RoutedEventArgs e)
        {
            var lista = Seleccionadas();
            if (lista.Count == 0) { Dialogo.Show("Selecciona al menos una persona."); return; }

            try
            {
                var frentes = new List<byte[]>();
                foreach (CredencialImportada p in lista)
                    frentes.Add(RenderPersona(p).frente);

                string ruta = RutaSalida("Lote_credenciales");
                new PdfReportService().GenerarCredencialesMosaico(frentes, ruta);

                new BitacoraService().Registrar("Generar credenciales (lote)",
                    $"{lista.Count} credenciales");

                Abrir(ruta);
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo generar el lote:\n\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string RutaSalida(string prefijo)
        {
            AppConfig.Actual.AsegurarCarpetas();
            return Path.Combine(
                AppConfig.Actual.CarpetaReportes,
                $"{prefijo}_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf");
        }

        private void Abrir(string ruta)
        {
            Process.Start(new ProcessStartInfo { FileName = ruta, UseShellExecute = true });
        }
    }
}
