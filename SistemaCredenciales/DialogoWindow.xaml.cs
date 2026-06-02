using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SistemaCredenciales
{
    /// <summary>
    /// Ventana de diálogo con el diseño de la app (reemplaza al MessageBox feo
    /// de Windows). Se usa a través del helper estático <see cref="Dialogo"/>.
    /// </summary>
    public partial class DialogoWindow : Window
    {
        public MessageBoxResult Resultado = MessageBoxResult.None;

        public DialogoWindow(
            string mensaje,
            string titulo,
            MessageBoxButton botones,
            MessageBoxImage icono)
        {
            InitializeComponent();

            txtMensaje.Text = mensaje;
            txtTitulo.Text = titulo;

            ConfigurarIcono(icono);
            ConfigurarBotones(botones);
        }

        private void ConfigurarIcono(MessageBoxImage icono)
        {
            string simbolo;
            Color fondo;

            switch (icono)
            {
                case MessageBoxImage.Warning:
                    simbolo = "⚠"; fondo = (Color)ColorConverter.ConvertFromString("#FEF3C7");
                    break;
                case MessageBoxImage.Error:
                    simbolo = "⛔"; fondo = (Color)ColorConverter.ConvertFromString("#FEE2E2");
                    break;
                case MessageBoxImage.Question:
                    simbolo = "❓"; fondo = (Color)ColorConverter.ConvertFromString("#DBEAFE");
                    break;
                default:
                    simbolo = "ℹ"; fondo = (Color)ColorConverter.ConvertFromString("#DBEAFE");
                    break;
            }

            txtIcono.Text = simbolo;
            iconoBg.Background = new SolidColorBrush(fondo);
        }

        private void ConfigurarBotones(MessageBoxButton botones)
        {
            if (botones == MessageBoxButton.YesNo
                || botones == MessageBoxButton.YesNoCancel)
            {
                if (botones == MessageBoxButton.YesNoCancel)
                    AgregarBoton("Cancelar", false, MessageBoxResult.Cancel, false, true);

                AgregarBoton("No", false, MessageBoxResult.No, false,
                    botones == MessageBoxButton.YesNo);

                AgregarBoton("Sí", true, MessageBoxResult.Yes, true, false);
            }
            else if (botones == MessageBoxButton.OKCancel)
            {
                AgregarBoton("Cancelar", false, MessageBoxResult.Cancel, false, true);
                AgregarBoton("Aceptar", true, MessageBoxResult.OK, true, false);
            }
            else
            {
                AgregarBoton("Aceptar", true, MessageBoxResult.OK, true, true);
            }
        }

        private void AgregarBoton(
            string texto,
            bool primario,
            MessageBoxResult resultado,
            bool esDefault,
            bool esCancelar)
        {
            var boton = new Button
            {
                Content = texto,
                MinWidth = 96,
                Margin = new Thickness(10, 0, 0, 0),
                IsDefault = esDefault,
                IsCancel = esCancelar
            };

            if (!primario)
                boton.Style = (Style)FindResource("GhostButton");

            boton.Click += (s, e) =>
            {
                Resultado = resultado;
                Close();
            };

            panelBotones.Children.Add(boton);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }

    /// <summary>
    /// Reemplazo de MessageBox con el estilo de la app. Misma firma para que el
    /// código existente funcione cambiando solo "MessageBox" por "Dialogo".
    /// </summary>
    public static class Dialogo
    {
        public static MessageBoxResult Show(string mensaje)
            => Show(mensaje, "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);

        public static MessageBoxResult Show(string mensaje, string titulo)
            => Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);

        public static MessageBoxResult Show(string mensaje, string titulo,
            MessageBoxButton botones)
            => Show(mensaje, titulo, botones, MessageBoxImage.Information);

        public static MessageBoxResult Show(string mensaje, string titulo,
            MessageBoxButton botones, MessageBoxImage icono)
        {
            var ventana = new DialogoWindow(mensaje, titulo, botones, icono);

            Window? owner = ObtenerOwner(ventana);

            if (owner != null)
            {
                ventana.Owner = owner;
                ventana.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                ventana.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            ventana.ShowDialog();
            return ventana.Resultado;
        }

        private static Window? ObtenerOwner(Window excepto)
        {
            if (Application.Current == null)
                return null;

            foreach (Window win in Application.Current.Windows)
            {
                if (win != excepto && win.IsActive)
                    return win;
            }

            return Application.Current.MainWindow != excepto
                ? Application.Current.MainWindow
                : null;
        }
    }
}
