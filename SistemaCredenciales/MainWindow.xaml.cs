using SistemaCredenciales.Models;
using SistemaCredenciales.Reports;
using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SistemaCredenciales
{
    public partial class MainWindow : Window
    {
        private const string TodasLasEscuelas = "— Todas —";

        // Credenciales de la escuela activa (o de todas si no hay filtro).
        private List<CredencialImportada> todasLasCredenciales =
            new List<CredencialImportada>();

        // Escuela seleccionada actualmente. Vacío = todas.
        private string escuelaActual = "";

        // Evita que rellenar el ComboBox dispare recargas en cascada.
        private bool cargandoCombo = false;

        public static MainWindow Instancia = null!;

        public MainWindow()
        {
            InitializeComponent();
            Instancia = this;

            // Asegura el config.json portable y las carpetas de firmas/reportes
            // junto al ejecutable desde el primer arranque.
            AppConfig.Actual.AsegurarCarpetas();

            CargarLogo();

            txtBuscar.Text = "Buscar matrícula o nombre...";

            CargarDatos();
        }

        /// <summary>
        /// Muestra el logo (logo.png junto al .exe) si existe; si no, deja el
        /// texto de respaldo. Se carga en memoria para no bloquear el archivo.
        /// </summary>
        private void CargarLogo()
        {
            try
            {
                if (!File.Exists(AppConfig.RutaLogo))
                    return;

                // OnLoad: lee el archivo de inmediato y no lo deja bloqueado.
                // IgnoreImageCache: vuelve a leer del disco (si cambia el logo,
                // se ve el nuevo y no la versión vieja en caché).
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(AppConfig.RutaLogo, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                imgLogo.Source = bmp;
                imgLogo.Visibility = Visibility.Visible;
                logoFallback.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // Si el logo no se puede leer, se queda el texto de respaldo.
            }
        }

        /// <summary>Recarga el logo (lo llama Configuración al cambiarlo).</summary>
        public void RecargarLogo()
        {
            imgLogo.Source = null;
            imgLogo.Visibility = Visibility.Collapsed;
            logoFallback.Visibility = Visibility.Visible;
            CargarLogo();
        }

        // ----------------------------------------------------------------
        //  CARGA Y FILTRADO POR ESCUELA
        // ----------------------------------------------------------------

        /// <summary>
        /// Recarga la lista desde la base aplicando la escuela activa, y
        /// actualiza el ComboBox y el tablero.
        /// </summary>
        public void CargarDatos()
        {
            DatabaseService db = new DatabaseService();

            string? filtro =
                escuelaActual == "" ? null : escuelaActual;

            todasLasCredenciales = db.ObtenerCredenciales(filtro);

            RefrescarComboEscuelas(db);

            dgEscuelas.ItemsSource = todasLasCredenciales;

            ActualizarDashboard();
        }

        /// <summary>Compatibilidad con código existente.</summary>
        public void RecargarCredenciales()
        {
            CargarDatos();
        }

        /// <summary>
        /// Fija la escuela a mostrar y recarga (la llama EscuelasWindow).
        /// </summary>
        public void SeleccionarEscuela(string escuela)
        {
            escuelaActual = escuela ?? "";
            CargarDatos();
        }

        private void RefrescarComboEscuelas(DatabaseService db)
        {
            cargandoCombo = true;

            List<string> escuelas = db.ObtenerEscuelas();

            cmbEscuelas.Items.Clear();
            cmbEscuelas.Items.Add(TodasLasEscuelas);

            foreach (string escuela in escuelas)
                cmbEscuelas.Items.Add(escuela);

            // Si la escuela activa ya no existe, volver a "Todas".
            if (escuelaActual != "" && !escuelas.Contains(escuelaActual))
                escuelaActual = "";

            cmbEscuelas.SelectedItem =
                escuelaActual == "" ? TodasLasEscuelas : escuelaActual;

            cargandoCombo = false;
        }

        private void cmbEscuelas_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (cargandoCombo)
                return;

            string seleccion =
                cmbEscuelas.SelectedItem as string ?? TodasLasEscuelas;

            escuelaActual =
                seleccion == TodasLasEscuelas ? "" : seleccion;

            CargarDatos();
        }

        private void ActualizarDashboard()
        {
            int total = todasLasCredenciales.Count;

            int entregadas =
                todasLasCredenciales.Count(x => x.Entregada);

            int pendientes = total - entregadas;

            double avance =
                total > 0 ? (double)entregadas / total * 100 : 0;

            txtTotal.Text = total.ToString();
            txtEntregadas.Text = entregadas.ToString();
            txtPendientes.Text = pendientes.ToString();
            txtAvance.Text = $"{avance:0}%";
        }

        // ----------------------------------------------------------------
        //  TARJETAS DEL TABLERO (filtran dentro de la escuela activa)
        // ----------------------------------------------------------------

        private void BtnTotal_Click(object sender, RoutedEventArgs e)
        {
            dgEscuelas.ItemsSource = todasLasCredenciales;
        }

        private void BtnEntregadas_Click(object sender, RoutedEventArgs e)
        {
            dgEscuelas.ItemsSource =
                todasLasCredenciales.Where(x => x.Entregada).ToList();
        }

        private void BtnPendientes_Click(object sender, RoutedEventArgs e)
        {
            dgEscuelas.ItemsSource =
                todasLasCredenciales.Where(x => !x.Entregada).ToList();
        }

        // ----------------------------------------------------------------
        //  BUSCADOR
        // ----------------------------------------------------------------

        private void txtBuscar_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            if (dgEscuelas == null)
                return;

            string texto = txtBuscar.Text.ToLower();

            var filtradas = todasLasCredenciales.Where(x =>
                (x.Matricula ?? "").ToLower().Contains(texto)
                || (x.Nombre ?? "").ToLower().Contains(texto)
                || (x.Apellidos ?? "").ToLower().Contains(texto)
            ).ToList();

            dgEscuelas.ItemsSource = filtradas;
        }

        private void txtBuscar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar matrícula o nombre...")
                txtBuscar.Text = "";
        }

        private void txtBuscar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
                txtBuscar.Text = "Buscar matrícula o nombre...";
        }

        // ----------------------------------------------------------------
        //  ENTREGA / CANCELACIÓN
        // ----------------------------------------------------------------

        private void BtnEntregar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEscuelas.SelectedItem == null)
            {
                Dialogo.Show("Selecciona una credencial.");
                return;
            }

            CredencialImportada credencial =
                (CredencialImportada)dgEscuelas.SelectedItem;

            if (credencial.Entregada)
            {
                Dialogo.Show("La credencial ya está entregada.");
                return;
            }

            DatabaseService db = new DatabaseService();

            FirmaWindow firma =
                new FirmaWindow(credencial.Matricula, credencial.Id);

            firma.ShowDialog();

            if (!firma.FirmaGuardada)
            {
                Dialogo.Show("Entrega cancelada.");
                return;
            }

            db.CambiarEstadoEntrega(credencial.Id, true);

            Dialogo.Show("Credencial entregada.");

            CargarDatos();
        }

        private void BtnCancelarEntrega_Click(object sender, RoutedEventArgs e)
        {
            if (dgEscuelas.SelectedItem == null)
            {
                Dialogo.Show("Selecciona una credencial.");
                return;
            }

            CredencialImportada credencial =
                (CredencialImportada)dgEscuelas.SelectedItem;

            if (!credencial.Entregada)
            {
                Dialogo.Show("La credencial ya está pendiente.");
                return;
            }

            MessageBoxResult resultado =
                Dialogo.Show(
                    "¿Seguro que deseas cancelar esta entrega?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes)
                return;

            DatabaseService db = new DatabaseService();

            string rutaFirma = db.ObtenerRutaFirma(credencial.Id);

            if (System.IO.File.Exists(rutaFirma))
                System.IO.File.Delete(rutaFirma);

            db.LimpiarRutaFirma(credencial.Id);
            db.CambiarEstadoEntrega(credencial.Id, false);

            Dialogo.Show("Entrega cancelada.");

            CargarDatos();
        }

        private void dgEscuelas_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgEscuelas.SelectedItem == null)
                return;

            CredencialImportada credencial =
                (CredencialImportada)dgEscuelas.SelectedItem;

            DatabaseService db = new DatabaseService();

            string rutaFirma = db.ObtenerRutaFirma(credencial.Id);

            if (string.IsNullOrEmpty(rutaFirma))
            {
                Dialogo.Show("Esta credencial no tiene firma.");
                return;
            }

            if (!System.IO.File.Exists(rutaFirma))
            {
                Dialogo.Show("No se encontró el archivo de firma.");
                return;
            }

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = rutaFirma,
                    UseShellExecute = true
                });
        }

        // ----------------------------------------------------------------
        //  NAVEGACIÓN DEL MENÚ LATERAL
        // ----------------------------------------------------------------

        private void BtnEscuelas_Click(object sender, RoutedEventArgs e)
        {
            new EscuelasWindow().ShowDialog();
        }

        private void BtnLotes_Click(object sender, RoutedEventArgs e)
        {
            new LotesWindow().ShowDialog();
            CargarDatos();
        }

        private void BtnEntregas_Click(object sender, RoutedEventArgs e)
        {
            new EntregasWindow().ShowDialog();
        }

        private void BtnReportes_Click(object sender, RoutedEventArgs e)
        {
            new ReportesWindow().ShowDialog();
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            new ConfiguracionWindow().ShowDialog();
            CargarDatos();
        }

        private void BtnMasivo_Click(object sender, RoutedEventArgs e)
        {
            var clave = new ClaveWindow(
                "Esta acción marca como ENTREGADAS (sin firma) todas las " +
                "credenciales pendientes de la vista actual. Escribe la contraseña.")
            {
                Owner = this
            };

            if (clave.ShowDialog() != true)
                return;

            if (clave.Clave != AppConfig.Actual.ClaveMaestra)
            {
                Dialogo.Show("Contraseña incorrecta.", "Acceso denegado",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int pendientes = todasLasCredenciales.Count(x => !x.Entregada);

            if (pendientes == 0)
            {
                Dialogo.Show("No hay credenciales pendientes en esta vista.");
                return;
            }

            string ambito =
                escuelaActual == ""
                    ? "TODAS las escuelas"
                    : $"la escuela \"{escuelaActual}\"";

            MessageBoxResult r = Dialogo.Show(
                $"Se marcarán como entregadas (sin firma) {pendientes} credenciales " +
                $"pendientes de {ambito}.\n\n¿Continuar?",
                "Marcar masivo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes)
                return;

            string? filtro = escuelaActual == "" ? null : escuelaActual;

            DatabaseService db = new DatabaseService();
            int n = db.MarcarTodasEntregadas(filtro);

            Dialogo.Show($"Listo: {n} credenciales marcadas como entregadas.");

            CargarDatos();
        }

        private void BtnQuitarEscuela_Click(object sender, RoutedEventArgs e)
        {
            if (escuelaActual == "")
            {
                Dialogo.Show(
                    "Primero selecciona una escuela en la lista de arriba " +
                    "(no se puede quitar \"Todas\").");
                return;
            }

            MessageBoxResult resultado = Dialogo.Show(
                $"¿Quitar la escuela \"{escuelaActual}\" y TODAS sus credenciales?\n\n" +
                "Esto borra también sus firmas. No se puede deshacer.",
                "Quitar escuela",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes)
                return;

            DatabaseService db = new DatabaseService();
            int eliminadas = db.EliminarEscuela(escuelaActual);

            Dialogo.Show($"Se quitó \"{escuelaActual}\" ({eliminadas} credenciales).");

            escuelaActual = "";
            CargarDatos();
        }
    }
}
