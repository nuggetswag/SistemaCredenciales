using SistemaCredenciales.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SistemaCredenciales
{
    public partial class FirmaWindow : Window
    {
        public bool FirmaGuardada = false;

        private string matricula;

        private int idCredencial;

        public FirmaWindow(string matriculaAlumno,
            int id)
        {
            InitializeComponent();

           

            inkFirma.EditingMode =
            System.Windows.Controls.InkCanvasEditingMode.Ink;

            inkFirma.Focus();

           
            matricula = matriculaAlumno;

            idCredencial = id;
        }

        private void BtnGuardar_Click(object sender,
            RoutedEventArgs e)
        {
            if (inkFirma.Strokes.Count == 0)
            {
                Dialogo.Show(
                    "Debes capturar una firma.");

                return;
            }


            RenderTargetBitmap render =
                new RenderTargetBitmap(
                    (int)inkFirma.ActualWidth,
                    (int)inkFirma.ActualHeight,
                    96d,
                    96d,
                    PixelFormats.Default);

            render.Render(inkFirma);

            PngBitmapEncoder encoder =
                new PngBitmapEncoder();

            encoder.Frames.Add(
                BitmapFrame.Create(render));

            string carpetaFirmas =
                AppConfig.Actual.CarpetaFirmas;

            Directory.CreateDirectory(carpetaFirmas);

            string ruta =
                Path.Combine(
                    carpetaFirmas,
                    $"{matricula}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");

            using (FileStream stream =
                new FileStream(ruta, FileMode.Create))
            {
                encoder.Save(stream);
            }

            DatabaseService db =
                new DatabaseService();

            db.GuardarRutaFirma(idCredencial, ruta);
            FirmaGuardada = true;

            Dialogo.Show(
                "Firma guardada correctamente.");

            Close();
        }

        private void BtnCancelar_Click(object sender,
    RoutedEventArgs e)
        {
            Close();
        }

        private void BtnLimpiar_Click(object sender,
    RoutedEventArgs e)
        {
            inkFirma.Strokes.Clear();
        }

        private void Window_KeyDown(object sender,
    KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                BtnGuardar_Click(sender, e);
            }

            if (e.Key == Key.Delete
    ||          e.Key == Key.Back)
            {
                e.Handled = true;

                inkFirma.Strokes.Clear();
            }

            if (e.Key == Key.Escape)
            {
                e.Handled = true;

                Close();
            }
        }


    }
}