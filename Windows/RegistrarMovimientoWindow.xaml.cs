using System;
using System.Collections.ObjectModel; // Para listas dinámicas (ObservableCollection)
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestorAlmacen.Views.Windows
{
    public partial class RegistrarMovimientoWindow : Window
    {
        // Mock para simular una línea del detalle
        public class DetalleItem
        {
            public string Sku { get; set; }
            public string NombreProducto { get; set; }
            public string AreaOrigen { get; set; }
            public string AreaDestino { get; set; }
            public int Cantidad { get; set; }
        }

        // Usamos ObservableCollection para que el DataGrid se actualice solo al agregar items
        private ObservableCollection<DetalleItem> _carritoCompras;

        public RegistrarMovimientoWindow()
        {
            InitializeComponent();

            lblFecha.Text = DateTime.Now.ToShortDateString();
            _carritoCompras = new ObservableCollection<DetalleItem>();
            dgDetalles.ItemsSource = _carritoCompras;

            CargarAreasMock();
        }

        private void CargarAreasMock()
        {
            // Simulamos carga de áreas
            string[] areas = { "A1", "A2", "B1", "RECEPCION", "MERMA" };
            cmbAreaOrigen.ItemsSource = areas;
            cmbAreaDestino.ItemsSource = areas;
        }

        // --- LÓGICA DE INTERFAZ DINÁMICA ---
        private void cmbTipoMovimiento_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Evitar error en inicialización
            if (pnlOrigen == null || pnlDestino == null) return;

            // Obtenemos el Tag del item seleccionado (ENTR, SAL, TRANS)
            ComboBoxItem item = (ComboBoxItem)cmbTipoMovimiento.SelectedItem;
            string tipo = item.Tag.ToString();

            // Reseteamos visibilidad
            pnlOrigen.Visibility = Visibility.Collapsed;
            pnlDestino.Visibility = Visibility.Collapsed;

            switch (tipo)
            {
                case "ENTR":
                    pnlDestino.Visibility = Visibility.Visible;
                    break;
                case "SAL":
                    pnlOrigen.Visibility = Visibility.Visible;
                    break;
                case "TRANS":
                    pnlOrigen.Visibility = Visibility.Visible;
                    pnlDestino.Visibility = Visibility.Visible;
                    break;
            }
        }

        // --- SIMULACIÓN DE BÚSQUEDA ---
        private void btnBuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            string sku = txtSkuBuscar.Text.ToUpper();
            if (string.IsNullOrEmpty(sku))
            {
                MessageBox.Show("Escriba un SKU para buscar.");
                return;
            }

            // Simulación: Si escribe algo, lo encontramos
            lblNombreProducto.Text = "Producto Simulado: " + sku;
            lblStockOrigen.Text = "Stock actual: 50"; // Dato dummy
        }

        // --- AGREGAR AL CARRITO ---
        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validaciones básicas de UI
            if (string.IsNullOrEmpty(txtSkuBuscar.Text)) return;

            if (!int.TryParse(txtCantidad.Text, out int cant) || cant <= 0)
            {
                MessageBox.Show("Cantidad inválida.");
                return;
            }

            string tipo = ((ComboBoxItem)cmbTipoMovimiento.SelectedItem).Tag.ToString();

            // Validar que se seleccionaron las áreas requeridas
            if ((tipo == "SAL" || tipo == "TRANS") && cmbAreaOrigen.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un Área de Origen.");
                return;
            }
            if ((tipo == "ENTR" || tipo == "TRANS") && cmbAreaDestino.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un Área de Destino.");
                return;
            }

            // 2. Crear el objeto
            var nuevoItem = new DetalleItem
            {
                Sku = txtSkuBuscar.Text.ToUpper(),
                NombreProducto = lblNombreProducto.Text,
                Cantidad = cant,
                AreaOrigen = (pnlOrigen.Visibility == Visibility.Visible) ? cmbAreaDestino.Text : "EXTERNO",
                // Nota: Arriba hay un pequeño bug lógico en mi simulación visual, corregimos:
                // Si es Origen visible, tomamos el combo origen.
            };

            // Ajuste fino de lógica visual para el objeto
            if (pnlOrigen.Visibility == Visibility.Visible)
                nuevoItem.AreaOrigen = cmbAreaOrigen.SelectedItem.ToString();
            else
                nuevoItem.AreaOrigen = "N/A"; // Es una entrada desde proveedor

            if (pnlDestino.Visibility == Visibility.Visible)
                nuevoItem.AreaDestino = cmbAreaDestino.SelectedItem.ToString();
            else
                nuevoItem.AreaDestino = "N/A"; // Es una salida hacia cliente

            // 3. Agregar a la lista
            _carritoCompras.Add(nuevoItem);

            // 4. Limpiar campos para el siguiente
            txtCantidad.Text = "";
            txtSkuBuscar.Focus();
            ActualizarTotales();
        }

        private void btnQuitarFila_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el objeto asociado al botón pulsado
            var item = ((Button)sender).DataContext as DetalleItem;
            _carritoCompras.Remove(item);
            ActualizarTotales();
        }

        private void ActualizarTotales()
        {
            txtTotalItems.Text = _carritoCompras.Sum(x => x.Cantidad).ToString();
        }

        private void btnGuardarTodo_Click(object sender, RoutedEventArgs e)
        {
            if (_carritoCompras.Count == 0)
            {
                MessageBox.Show("No hay items en el movimiento.");
                return;
            }

            MessageBox.Show($"¡Listo para guardar en BD!\nSe generará folio tipo: {((ComboBoxItem)cmbTipoMovimiento.SelectedItem).Tag}");
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}