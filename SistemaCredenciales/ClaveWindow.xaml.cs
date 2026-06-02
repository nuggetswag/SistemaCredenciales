using System.Windows;
using System.Windows.Input;

namespace SistemaCredenciales
{
    /// <summary>
    /// Pide una contraseña con el estilo de la app. Devuelve true en ShowDialog
    /// si el usuario aceptó; la contraseña queda en <see cref="Clave"/>.
    /// </summary>
    public partial class ClaveWindow : Window
    {
        public string Clave { get; private set; } = "";

        public ClaveWindow(string mensaje = "")
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(mensaje))
                txtMensaje.Text = mensaje;

            Loaded += (s, e) => pwd.Focus();
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            Clave = pwd.Password;
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
