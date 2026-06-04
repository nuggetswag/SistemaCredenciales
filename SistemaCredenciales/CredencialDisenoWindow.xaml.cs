using Microsoft.Win32;
using SistemaCredenciales.Models;
using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SistemaCredenciales
{
    public partial class CredencialDisenoWindow : Window
    {
        private readonly DisenoService disenoService = new DisenoService();
        private DisenoCredencial diseno = new DisenoCredencial();

        private readonly Dictionary<CampoCredencial, Border> boxes =
            new Dictionary<CampoCredencial, Border>();
        private CampoCredencial? seleccionado;

        private double escala = 1.0;
        private int tplW = CredencialRenderService.DefaultW;
        private int tplH = CredencialRenderService.DefaultH;

        private bool cargando = false;

        // Estado de arrastre
        private Border? arrastrando;
        private Point offset;

        public CredencialDisenoWindow()
        {
            InitializeComponent();

            // Categorías: las que ya existen en datos + las que tienen diseño +
            // las predefinidas (sugerencias). El campo es editable: puedes escribir
            // una categoría nueva.
            var cats = new List<string>(DisenoService.Tipos);
            foreach (string c in new DatabaseService().ObtenerCategorias())
                if (!cats.Contains(c)) cats.Add(c);
            foreach (string c in disenoService.TiposConDiseno())
                if (!cats.Contains(c)) cats.Add(c);

            foreach (string c in cats) cmbTipo.Items.Add(c);
            foreach (string l in DisenoService.Lados) cmbLado.Items.Add(l);
            cmbAlign.Items.Add("left");
            cmbAlign.Items.Add("center");
            cmbAlign.Items.Add("right");

            cmbTipo.SelectedIndex = 0;
            cmbLado.SelectedIndex = 0;

            CargarDiseno();
        }

        private string Tipo =>
            string.IsNullOrWhiteSpace(cmbTipo.Text) ? "Alumno" : cmbTipo.Text.Trim();

        private void Tipo_Commit(object sender, RoutedEventArgs e) => RecargarSiCambioTipo();

        private void Tipo_DropClosed(object? sender, EventArgs e) => RecargarSiCambioTipo();

        private void RecargarSiCambioTipo()
        {
            if (!cargando && IsLoaded && Tipo != diseno.Tipo)
                CargarDiseno();
        }
        private string Lado => cmbLado.SelectedItem as string ?? "frente";

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!cargando && IsLoaded)
                CargarDiseno();
        }

        private void CargarDiseno()
        {
            diseno = disenoService.Obtener(Tipo, Lado);
            seleccionado = null;
            panelProps.IsEnabled = false;
            lblCampo.Text = "(ninguno seleccionado)";

            CargarPlantilla(diseno.PlantillaRuta);
            ReconstruirBoxes();
        }

        private void CargarPlantilla(string ruta)
        {
            // Mismo tamaño de lienzo normalizado que usa el render.
            var (w, h) = CredencialRenderService.TamanoCanvas(ruta);
            tplW = w;
            tplH = h;

            if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    imgPlantilla.Source = bmp;
                }
                catch { imgPlantilla.Source = null; }
            }
            else
            {
                imgPlantilla.Source = null;
            }

            // Escala para que quepa en el área del lienzo.
            escala = Math.Min(Math.Min(760.0 / tplW, 600.0 / tplH), 1.5);

            lienzoHost.Width = tplW * escala;
            lienzoHost.Height = tplH * escala;
            lienzo.Width = tplW * escala;
            lienzo.Height = tplH * escala;
        }

        private void ReconstruirBoxes()
        {
            lienzo.Children.Clear();
            boxes.Clear();

            foreach (CampoCredencial c in diseno.Campos)
                CrearBox(c);
        }

        private void CrearBox(CampoCredencial c)
        {
            var box = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(70, 37, 99, 235)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                BorderThickness = new Thickness(1.5),
                Cursor = Cursors.SizeAll,
                Child = new TextBlock
                {
                    FontSize = 11,
                    Foreground = Brushes.White,
                    Margin = new Thickness(3, 1, 3, 1),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            box.MouseLeftButtonDown += Box_MouseDown;
            box.MouseMove += Box_MouseMove;
            box.MouseLeftButtonUp += Box_MouseUp;

            lienzo.Children.Add(box);
            boxes[c] = box;
            RefrescarBox(c);
        }

        private void RefrescarBox(CampoCredencial c)
        {
            if (!boxes.TryGetValue(c, out Border? box))
                return;

            double w = Math.Max(c.Width, 20) * escala;
            double h = c.Kind == "photo"
                ? Math.Max(c.Height, 20) * escala
                : Math.Max(c.FontSize * 1.3, 16) * escala;

            box.Width = w;
            box.Height = h;
            Canvas.SetLeft(box, c.X * escala);
            Canvas.SetTop(box, c.Y * escala);

            ((TextBlock)box.Child).Text = EtiquetaDe(c);
            box.BorderThickness = new Thickness(seleccionado == c ? 3 : 1.5);
            box.BorderBrush = new SolidColorBrush(seleccionado == c
                ? Color.FromRgb(217, 119, 6)   // ámbar si seleccionado
                : Color.FromRgb(37, 99, 235));
        }

        private string EtiquetaDe(CampoCredencial c)
        {
            if (c.Kind == "photo") return "📷 Foto";
            if (!string.IsNullOrEmpty(c.Clave)) return c.Clave;
            return string.IsNullOrEmpty(c.Texto) ? "Texto" : c.Texto;
        }

        // ---------------- Arrastre ----------------

        private void Box_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var box = (Border)sender;
            var campo = CampoDe(box);
            if (campo == null) return;

            Seleccionar(campo);
            arrastrando = box;
            offset = e.GetPosition(box);
            box.CaptureMouse();
            e.Handled = true;
        }

        private void Box_MouseMove(object sender, MouseEventArgs e)
        {
            if (arrastrando == null || sender != arrastrando)
                return;

            Point p = e.GetPosition(lienzo);
            double left = Math.Max(0, p.X - offset.X);
            double top = Math.Max(0, p.Y - offset.Y);

            Canvas.SetLeft(arrastrando, left);
            Canvas.SetTop(arrastrando, top);
        }

        private void Box_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (arrastrando == null) return;

            var campo = CampoDe(arrastrando);
            if (campo != null)
            {
                campo.X = Canvas.GetLeft(arrastrando) / escala;
                campo.Y = Canvas.GetTop(arrastrando) / escala;
            }

            arrastrando.ReleaseMouseCapture();
            arrastrando = null;
        }

        private CampoCredencial? CampoDe(Border box)
        {
            foreach (var kv in boxes)
                if (kv.Value == box) return kv.Key;
            return null;
        }

        private void Lienzo_Click(object sender, MouseButtonEventArgs e)
        {
            // Clic en zona vacía = deseleccionar
            if (e.OriginalSource == lienzo)
                Seleccionar(null);
        }

        // ---------------- Selección y propiedades ----------------

        private void Seleccionar(CampoCredencial? c)
        {
            seleccionado = c;
            foreach (var kv in boxes) RefrescarBox(kv.Key);

            if (c == null)
            {
                panelProps.IsEnabled = false;
                lblCampo.Text = "(ninguno seleccionado)";
                return;
            }

            cargando = true;
            panelProps.IsEnabled = true;
            lblCampo.Text = "Campo: " + EtiquetaDe(c);
            txtTexto.Text = c.Texto;
            txtFontSize.Text = c.FontSize.ToString(CultureInfo.InvariantCulture);
            txtColor.Text = c.Color;
            cmbAlign.SelectedItem = c.Align;
            chkBold.IsChecked = c.Bold;
            txtAncho.Text = c.Width.ToString(CultureInfo.InvariantCulture);
            txtAlto.Text = c.Height.ToString(CultureInfo.InvariantCulture);
            lblAlto.Visibility = c.Kind == "photo" ? Visibility.Visible : Visibility.Collapsed;
            txtAlto.Visibility = c.Kind == "photo" ? Visibility.Visible : Visibility.Collapsed;
            txtTexto.Visibility = (c.Kind == "text" && string.IsNullOrEmpty(c.Clave))
                ? Visibility.Visible : Visibility.Collapsed;
            cargando = false;
        }

        private void Prop_Changed(object sender, RoutedEventArgs e)
        {
            if (cargando || seleccionado == null) return;

            seleccionado.Texto = txtTexto.Text;
            seleccionado.FontSize = ParseD(txtFontSize.Text, seleccionado.FontSize);
            seleccionado.Color = string.IsNullOrWhiteSpace(txtColor.Text) ? "#000000" : txtColor.Text.Trim();
            seleccionado.Align = cmbAlign.SelectedItem as string ?? "left";
            seleccionado.Bold = chkBold.IsChecked == true;
            seleccionado.Width = ParseD(txtAncho.Text, seleccionado.Width);
            seleccionado.Height = ParseD(txtAlto.Text, seleccionado.Height);

            RefrescarBox(seleccionado);
        }

        private double ParseD(string s, double def)
        {
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                ? v : def;
        }

        // ---------------- Agregar / eliminar ----------------

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button)?.Tag as string ?? "TEXTO";

            var c = new CampoCredencial
            {
                X = tplW * 0.1,
                Y = tplH * 0.1,
                Layer = diseno.Campos.Count
            };

            if (tag == "FOTO")
            {
                c.Kind = "photo";
                c.Width = 300; c.Height = 380;
            }
            else if (tag == "TEXTO")
            {
                c.Kind = "text"; c.Clave = ""; c.Texto = "Texto";
                c.Width = 300; c.FontSize = 28;
            }
            else
            {
                c.Kind = "text"; c.Clave = tag;
                c.Width = 380; c.FontSize = 28;
            }

            diseno.Campos.Add(c);
            CrearBox(c);
            Seleccionar(c);
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (seleccionado == null) return;

            if (boxes.TryGetValue(seleccionado, out Border? box))
            {
                lienzo.Children.Remove(box);
                boxes.Remove(seleccionado);
            }
            diseno.Campos.Remove(seleccionado);
            Seleccionar(null);
        }

        // ---------------- Plantilla / guardar / preview ----------------

        private void BtnPlantilla_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Selecciona la plantilla de la credencial",
                Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };
            if (dlg.ShowDialog() != true) return;

            diseno.PlantillaRuta = dlg.FileName;
            CargarPlantilla(diseno.PlantillaRuta);
            ReposicionarTodo();
        }

        private void ReposicionarTodo()
        {
            foreach (var kv in boxes) RefrescarBox(kv.Key);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            diseno.Tipo = Tipo;
            diseno.Lado = Lado;
            disenoService.Guardar(diseno);

            new BitacoraService().Registrar("Diseño de credencial",
                $"{Tipo} / {Lado}");

            Dialogo.Show("Diseño guardado. ✓", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            var ejemplo = new CredencialImportada
            {
                Matricula = "00000",
                Nombre = "NOMBRE EJEMPLO",
                Apellidos = "APELLIDO APELLIDO",
                Carrera = "CARRERA DE EJEMPLO",
                Vigencia = "MAR 26-SEP 26",
                Categoria = Tipo,
                Escuela = "ESCUELA"
            };

            try
            {
                byte[] png = new CredencialRenderService()
                    .RenderLado(ejemplo, diseno, null);

                string ruta = Path.Combine(
                    Path.GetTempPath(),
                    $"preview_credencial_{DateTime.Now:HHmmss}.png");
                File.WriteAllBytes(ruta, png);

                Process.Start(new ProcessStartInfo { FileName = ruta, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo generar la vista previa:\n\n" + ex.Message);
            }
        }
    }
}
