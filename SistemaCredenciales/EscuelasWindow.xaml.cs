using SistemaCredenciales.Services;
using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
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
            string escritorio =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop);

            string[] archivosMDB =
                Directory.GetFiles(
                    escritorio,
                    "*.mdb");

            foreach (string archivo in archivosMDB)
            {
                string nombreArchivo =
                    Path.GetFileNameWithoutExtension(
                        archivo);

                Button boton =
                    new Button();

                boton.Content =
                    "🏫 " + nombreArchivo;

                boton.Width = 300;
                boton.Height = 100;
                boton.Margin = new Thickness(10);
                boton.FontSize = 20;

                boton.Click += (s, e) =>
                {
                    string tablaMDB =
                        ObtenerTablaMDB(archivo);

                    CargarEscuela(
                        Path.GetFileName(archivo),
                        tablaMDB,
                        nombreArchivo);
                };

                panelEscuelas.Children.Add(boton);
            }
        }

        private string ObtenerTablaMDB(
            string rutaMDB)
        {
            string connectionString =
                $@"Provider=Microsoft.ACE.OLEDB.12.0;
                Data Source={rutaMDB};";

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                DataTable tablas =
                    connection.GetSchema("Tables");

                foreach (DataRow row in tablas.Rows)
                {
                    string nombreTabla =
                        row["TABLE_NAME"].ToString();

                    if (!nombreTabla.StartsWith("MSys"))
                    {
                        return nombreTabla;
                    }
                }
            }

            return "";
        }

        private void CargarEscuela(
            string archivoMDB,
            string tablaMDB,
            string area)
        {
            string escritorio =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop);

            string rutaMDB =
                Path.Combine(
                    escritorio,
                    archivoMDB);

            DatabaseService db =
                new DatabaseService();

            db.LeerMDB(
                rutaMDB,
                tablaMDB,
                area);

            MainWindow.Instancia
                .RecargarCredenciales();

            MessageBox.Show(
                $"{area} cargada correctamente 😎");

            Close();
        }
    }
}