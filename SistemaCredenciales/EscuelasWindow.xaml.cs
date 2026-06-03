using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaCredenciales
{
    public partial class EscuelasWindow : Window
    {
        public EscuelasWindow()
        {
            InitializeComponent();

            CargarEscuelas();
        }

        private void CargarEscuelas()
        {
            // Buscar archivos .mdb en las carpetas configuradas (por defecto el
            // escritorio, en cualquiera de sus variantes incluido OneDrive).
            var archivosMDB = new List<string>();

            foreach (string carpeta in AppConfig.Actual.CarpetasDeBusqueda())
            {
                try
                {
                    archivosMDB.AddRange(
                        Directory.GetFiles(carpeta, "*.mdb"));
                }
                catch
                {
                    // Si una carpeta no se puede leer, se ignora y se sigue.
                }
            }

            archivosMDB = archivosMDB
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => Path.GetFileName(a))
                .ToList();

            if (archivosMDB.Count == 0)
            {
                Dialogo.Show(
                    "No se encontraron archivos .mdb en la carpeta de búsqueda.\n\n" +
                    "Coloca las bases de las escuelas en el escritorio, o cambia la " +
                    "carpeta de búsqueda en Configuración.");
            }

            // Colores que se van alternando para el ícono de cada tarjeta.
            string[] colores =
                { "#2563EB", "#16A34A", "#7C3AED", "#DC2626", "#0EA5E9", "#D97706" };

            int indice = 0;

            foreach (string archivo in archivosMDB)
            {
                string nombreArchivo =
                    Path.GetFileNameWithoutExtension(archivo);

                string color = colores[indice % colores.Length];
                indice++;

                var boton = new Button
                {
                    Style = (Style)FindResource("EscuelaCard"),
                    Width = 300,
                    Height = 96,
                    Margin = new Thickness(0, 0, 16, 16),
                    Content = CrearContenidoTarjeta(nombreArchivo, color)
                };

                string rutaArchivo = archivo;

                boton.Click += (s, e) =>
                {
                    CargarEscuela(rutaArchivo, nombreArchivo);
                };

                panelEscuelas.Children.Add(boton);
            }
        }

        /// <summary>Construye el contenido visual de una tarjeta de escuela.</summary>
        private UIElement CrearContenidoTarjeta(string nombre, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);

            var fila = new StackPanel { Orientation = Orientation.Horizontal };

            var icono = new Border
            {
                Width = 52,
                Height = 52,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "🏫",
                    FontSize = 26,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var textos = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };

            textos.Children.Add(new TextBlock
            {
                Text = nombre,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#0F172A")),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 190
            });

            textos.Children.Add(new TextBlock
            {
                Text = "Abrir escuela  →",
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#64748B")),
                Margin = new Thickness(0, 4, 0, 0)
            });

            fila.Children.Add(icono);
            fila.Children.Add(textos);

            return fila;
        }

        private void CargarEscuela(string rutaMDB, string escuela)
        {
            try
            {
                DatabaseService db = new DatabaseService();

                string tablaMDB = db.ObtenerTablaMDB(rutaMDB);

                if (string.IsNullOrEmpty(tablaMDB))
                {
                    Dialogo.Show(
                        "No se encontró ninguna tabla de datos en esa base.");

                    return;
                }

                int insertadas = db.LeerMDB(rutaMDB, tablaMDB, escuela);

                new BitacoraService().Registrar("Importar escuela",
                    $"{escuela}: {insertadas} nuevas");

                // Mostrar SOLO esta escuela en la ventana principal.
                MainWindow.Instancia.SeleccionarEscuela(escuela);

                Dialogo.Show(
                    insertadas > 0
                        ? $"{escuela}: se agregaron {insertadas} credenciales nuevas. 😎"
                        : $"{escuela} ya estaba cargada (sin credenciales nuevas).");

                Close();
            }
            catch (Exception ex)
            {
                Dialogo.Show(
                    "No se pudo leer la base de la escuela.\n\n" + ex.Message);
            }
        }
    }
}
