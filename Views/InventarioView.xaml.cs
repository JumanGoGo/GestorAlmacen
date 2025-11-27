using System.Collections.Generic;
using System.Linq; // Necesario para filtrar listas (Where, ToList)
using System.Windows;
using System.Windows.Controls;

namespace GestorAlmacen.Views
{
    public partial class InventarioView : UserControl
    {
        // Mock que simula la vista unificada (JOIN de Stock, Products, Areas)
        public class StockItemMock
        {
            public string Sku { get; set; }
            public string NombreProducto { get; set; }
            public string Categoria { get; set; }
            public string CodigoArea { get; set; } // A1, A2...
            public int Cantidad { get; set; }
        }

        // Lista maestra para guardar los datos originales
        private List<StockItemMock> _inventarioCompleto;

        public InventarioView()
        {
            InitializeComponent();
            CargarDatosPrueba();
            CargarAreasCombo(); // Simulamos llenar el combo
        }

        private void CargarDatosPrueba()
        {
            // Simulamos datos de la base de datos
            // NOTA: Fíjate como el iPhone (CEL-001) aparece dos veces en áreas distintas
            _inventarioCompleto = new List<StockItemMock>
            {
                new StockItemMock { CodigoArea="A1", Sku="CEL-001", NombreProducto="iPhone 15 Pro", Categoria="Celulares", Cantidad=20 },
                new StockItemMock { CodigoArea="B3", Sku="CEL-001", NombreProducto="iPhone 15 Pro", Categoria="Celulares", Cantidad=5 },

                new StockItemMock { CodigoArea="A2", Sku="TV-002", NombreProducto="Samsung 55 4K", Categoria="Televisiones", Cantidad=10 },

                new StockItemMock { CodigoArea="C1", Sku="LBL-005", NombreProducto="Lavadora Whirlpool", Categoria="Línea Blanca", Cantidad=3 },

                new StockItemMock { CodigoArea="E5", Sku="AUD-009", NombreProducto="Audífonos Sony", Categoria="Audio", Cantidad=50 }
            };

            // Al inicio mostramos todo
            AplicarFiltros();
        }

        private void CargarAreasCombo()
        {
            // En el futuro esto viene de: SELECT code FROM Areas
            string[] areas = { "A1", "A2", "B1", "B2", "B3", "C1", "E5" };

            foreach (var area in areas)
            {
                cmbAreas.Items.Add(area);
            }
        }

        // Este evento se dispara cada vez que escribes en la caja o cambias el combo
        private void Filtros_Changed(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void btnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Text = "";
            cmbAreas.SelectedIndex = 0; // "Todas"
            CargarDatosPrueba();
        }

        private void AplicarFiltros()
        {
            if (_inventarioCompleto == null) return;

            var listaFiltrada = _inventarioCompleto.AsEnumerable();

            // 1. Filtro por Texto (SKU o Nombre)
            string texto = txtBuscar.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                listaFiltrada = listaFiltrada.Where(x => x.Sku.ToLower().Contains(texto) ||
                                                         x.NombreProducto.ToLower().Contains(texto));
            }

            // 2. Filtro por Área
            if (cmbAreas.SelectedItem != null)
            {
                // Si es un ComboBoxItem (como el primero "Todas"), obtenemos su contenido
                // Si es un string directo (los que agregamos por código), lo usamos directo
                string areaSeleccionada = "";

                if (cmbAreas.SelectedItem is ComboBoxItem item)
                    areaSeleccionada = item.Content.ToString();
                else
                    areaSeleccionada = cmbAreas.SelectedItem.ToString();

                if (areaSeleccionada != "Todas")
                {
                    listaFiltrada = listaFiltrada.Where(x => x.CodigoArea == areaSeleccionada);
                }
            }

            // Actualizar la Tabla
            var resultadoFinal = listaFiltrada.ToList();
            dgStock.ItemsSource = resultadoFinal;

            // Actualizar el Resumen (Suma de cantidades visibles)
            txtTotalCantidad.Text = resultadoFinal.Sum(x => x.Cantidad).ToString();
        }
    }
}