using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestorAlmacen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _rolUsuario;

        public MainWindow(string rol)
        {
            InitializeComponent();
            _rolUsuario = rol;
            AplicarPermisos();
            // Cargar Inventario por defecto
            MainContent.Navigate(new Views.InventarioView());
        }

        private void AplicarPermisos()
        {
            if (_rolUsuario == "Usuario")
            {
                // Ejemplo: Usuario básico no ve o no puede entrar a productos
                btnProductos.Visibility = Visibility.Collapsed;
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            string tag = ((Button)sender).Tag.ToString();
            switch (tag)
            {
                case "Productos": MainContent.Navigate(new Views.ProductosView()); break;
                case "Entradas": MainContent.Navigate(new Views.EntradasView()); break;
                    // ... resto de casos ...
            }
        }
    }
}
