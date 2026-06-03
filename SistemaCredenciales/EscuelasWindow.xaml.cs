using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

            foreach (string archivo in archivosMDB)
            {
                string nombreArchivo =
                    Path.GetFileNameWithoutExtension(archivo);

                Button boton = new Button
                {
                    Content = "🏫 " + nombreArchivo,
                    Width = 300,
                    Height = 100,
                    Margin = new Thickness(10),
                    FontSize = 20
                };

                string rutaArchivo = archivo;

                boton.Click += (s, e) =>
                {
                    CargarEscuela(rutaArchivo, nombreArchivo);
                };

                panelEscuelas.Children.Add(boton);
            }
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
