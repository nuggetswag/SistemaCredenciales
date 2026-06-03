using SistemaCredenciales.Services;
using System.Windows;

namespace SistemaCredenciales
{
    public partial class BitacoraWindow : Window
    {
        public BitacoraWindow()
        {
            InitializeComponent();

            dgBitacora.ItemsSource = new BitacoraService().Obtener();
        }
    }
}
