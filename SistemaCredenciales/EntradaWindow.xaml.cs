using System.Windows;
using System.Windows.Input;

namespace SistemaCredenciales
{
    /// <summary>Pide un texto con el estilo de la app.</summary>
    public partial class EntradaWindow : Window
    {
        public string Valor { get; private set; } = "";

        public EntradaWindow(string titulo, string mensaje, string valorInicial = "")
        {
            InitializeComponent();
            txtTitulo.Text = titulo;
            txtMensaje.Text = mensaje;
            txtValor.Text = valorInicial;
            Loaded += (s, e) => { txtValor.Focus(); txtValor.SelectAll(); };
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            Valor = txtValor.Text.Trim();
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
