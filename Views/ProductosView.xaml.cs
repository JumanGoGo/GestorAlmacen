using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GestorAlmacen.Views
{
    public partial class ProductosView : UserControl
    {
        // Esta clase interna simula tu Modelo final. 
        // Cuando conectemos la BD, borraremos esto y usaremos GestorAlmacen.Models.Producto
        public class ProductoMock
        {
            public int Id { get; set; }
            public string Sku { get; set; }
            public string Nombre { get; set; }
            public string Categoria { get; set; }
            public int StockTotal { get; set; }
            public bool IsActive { get; set; }
        }

        public ProductosView()
        {
            InitializeComponent();
            CargarDatosPrueba();
        }

        private void CargarDatosPrueba()
        {
            // Simulamos datos que vendrían de SQL Server
            List<ProductoMock> lista = new List<ProductoMock>
            {
                new ProductoMock { Id=1, Sku="CEL-001", Nombre="iPhone 15 Pro", Categoria="Celulares", StockTotal=50, IsActive=true },
                new ProductoMock { Id=2, Sku="TV-002", Nombre="Samsung 55' 4K", Categoria="Televisiones", StockTotal=12, IsActive=true },
                new ProductoMock { Id=3, Sku="LBL-003", Nombre="Refrigerador LG", Categoria="Línea Blanca", StockTotal=5, IsActive=true },
                new ProductoMock { Id=4, Sku="AUD-004", Nombre="Bocina Bose", Categoria="Audio", StockTotal=20, IsActive=false }, // Inactivo
            };

            dgProductos.ItemsSource = lista;
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Buscando: {txtBuscar.Text} (Lógica pendiente)");
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            // Instanciamos la ventana modal (Asegúrate de importar el namespace de Windows)
            var form = new GestorAlmacen.Views.Windows.ProductoFormWindow();

            // ShowDialog detiene la ejecución aquí hasta que el usuario cierre la ventana
            bool? resultado = form.ShowDialog();

            if (resultado == true)
            {
                // El usuario dio click en "Guardar" y pasó las validaciones
                MessageBox.Show("Refrescando lista de productos...");
                // Aquí llamaríamos de nuevo a CargarDatosPrueba() o CargarDesdeBD()
            }
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            // Validar que haya algo seleccionado
            if (dgProductos.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un producto para editar.");
                return;
            }

            // Obtener el objeto seleccionado
            ProductoMock seleccionado = (ProductoMock)dgProductos.SelectedItem;
            MessageBox.Show($"Editando SKU: {seleccionado.Sku}");
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProductos.SelectedItem == null) return;
            MessageBox.Show("Lógica de Soft Delete (IsActive = false)");
        }
    }
}