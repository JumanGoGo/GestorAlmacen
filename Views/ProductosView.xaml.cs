using System; 
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using GestorAlmacen.Models;
using System.Data.Entity;
using System.Transactions;

namespace GestorAlmacen.Views
{
    public partial class ProductosView : UserControl
    {
        public ProductosView()
        {
            InitializeComponent();

            // Verificamos si el usuario es ADMIN para mostrar el botón de Borrado Físico
            if (App.UsuarioActual != null && App.UsuarioActual.role == "ADMIN")
            {
                if (FindName("btnHardDelete") is Button btn)
                {
                    btn.Visibility = Visibility.Visible;
                }
            }

            CargarProductos();
        }

        private void CargarProductos(string filtroBusqueda = null)
        {
            using (var db = new WMS_DBEntities())
            {
                var query = db.Products
                      .Include("Category")
                      .Include("Stocks")
                      .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtroBusqueda))
                {
                    string filtro = filtroBusqueda.ToLower();
                    query = query.Where(p => p.sku.ToLower().Contains(filtro) ||
                                             p.name.ToLower().Contains(filtro));
                }

                var lista = query
                      .OrderByDescending(p => p.is_active)
                      .ThenBy(p => p.sku)
                      .ToList();

                dgProductos.ItemsSource = lista;
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var win = new Windows.ProductoFormWindow(null);
            if (win.ShowDialog() == true) CargarProductos();
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProductos.SelectedItem is Product prod)
            {
                var win = new Windows.ProductoFormWindow(prod.product_id);
                if (win.ShowDialog() == true) CargarProductos();
            }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            // Buscamos usando el nombre del TextBox definido en XAML
            if (FindName("txtBuscar") is TextBox searchBox)
            {
                CargarProductos(searchBox.Text);
            }
        }

        // --- SOFT DELETE (DESACTIVAR) ---
        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProductos.SelectedItem is Product productoSeleccionado)
            {
                using (var db = new WMS_DBEntities())
                {
                    bool tieneStockActivo = db.Stocks.Any(s => s.product_id == productoSeleccionado.product_id && s.quantity > 0);

                    if (tieneStockActivo)
                    {
                        MessageBox.Show("No se puede desactivar el producto. Existe stock activo asociado.",
                                        "Bloqueo de Integridad", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    if (MessageBox.Show($"¿Desactivar el producto: {productoSeleccionado.name}?",
                                        "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var prod = db.Products.Find(productoSeleccionado.product_id);
                        if (prod != null)
                        {
                            prod.is_active = false;
                            db.SaveChanges();
                            MessageBox.Show("Producto desactivado exitosamente.");
                            CargarProductos();
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto de la lista.");
            }
        }

        // --- HARD DELETE (BORRADO FÍSICO) ---
        private void btnHardDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgProductos.SelectedItem is Product prodSeleccionado)
            {
                var confirm = MessageBox.Show(
                    $"⚠️ ¡PELIGRO! BORRADO FÍSICO IRREVERSIBLE.\n\n" +
                    $"Se eliminará: {prodSeleccionado.name} y TODO su historial.\n\n" +
                    $"¿Está seguro?", "CONFIRMACIÓN CRÍTICA", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (confirm != MessageBoxResult.Yes) return;

                using (var scope = new TransactionScope())
                {
                    using (var db = new WMS_DBEntities())
                    {
                        try
                        {
                            var prodToDelete = db.Products.Find(prodSeleccionado.product_id);

                            if (prodToDelete != null)
                            {
                                // --- CORRECCIÓN AQUÍ: Usamos foreach en lugar de RemoveRange ---

                                // 1. Eliminar Detalles de Movimientos
                                var detalles = db.MovementDetails.Where(d => d.product_id == prodToDelete.product_id).ToList();
                                foreach (var d in detalles)
                                {
                                    db.MovementDetails.Remove(d);
                                }

                                // 2. Eliminar Stocks
                                var stocks = db.Stocks.Where(s => s.product_id == prodToDelete.product_id).ToList();
                                foreach (var s in stocks)
                                {
                                    db.Stocks.Remove(s);
                                }

                                // 3. Eliminar Producto
                                db.Products.Remove(prodToDelete);

                                db.SaveChanges();
                                scope.Complete();

                                MessageBox.Show("Registro eliminado físicamente de la base de datos.");
                                CargarProductos();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error crítico al borrar: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}