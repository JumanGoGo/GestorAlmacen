using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;

namespace GestorAlmacen.Views
{
    public partial class ProductosView : UserControl
    {
        public ProductosView()
        {
            InitializeComponent();
            CargarProductos();
        }

        private void CargarProductos()
        {
            using (var db = new WMS_DBEntities())
            {
                // Traemos los productos e incluimos la Categoría para mostrar el nombre
                var lista = db.Products.Include("Category").ToList();

                // NOTA: Asegúrate de que en el XAML el DataGrid binding de categoría sea: Binding="{Binding Categories.name}"
                dgProductos.ItemsSource = lista;
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var win = new Windows.ProductoFormWindow(null); // null = Nuevo
            if (win.ShowDialog() == true) CargarProductos();
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProductos.SelectedItem is Product prod)
            {
                var win = new Windows.ProductoFormWindow(prod.product_id); // Pasamos ID
                if (win.ShowDialog() == true) CargarProductos();
            }
        }

        // ... Implementa Delete y Buscar de forma similar a Categorías ...
        private void btnEliminar_Click(object sender, RoutedEventArgs e) { } // Pendiente implementar soft delete
        private void btnBuscar_Click(object sender, RoutedEventArgs e) { }
    }
}