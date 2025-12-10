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
                var lista = db.Products
                      .Include("Category")
                      .Include("Stocks") // <--- IMPORTANTE: Cargar los stocks relacionados
                      .ToList();

                if (lista.Count > 0)
                {
                    var p = lista.First();
                    // Intenta acceder a la propiedad. 
                    // Si esta línea marca ERROR ROJO al pegar, es que VS no une los archivos.
                    var stockPrueba = p.StockCalculado;

                    // Si compila, esto nos dirá si es problema de datos (0) o de interfaz.
                    // MessageBox.Show($"Prueba: El producto {p.sku} tiene stock calculado: {stockPrueba}");
                }

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