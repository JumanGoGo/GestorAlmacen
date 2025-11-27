using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Views.Windows; // Para poder abrir la ventana modal

namespace GestorAlmacen.Views
{
    public partial class MovimientosView : UserControl
    {
        // Mock para la tabla
        public class MovimientoMock
        {
            public int Id { get; set; }
            public string Folio { get; set; }
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } // ENTRADA, SALIDA, TRANS
            public string Usuario { get; set; }
            public string Estatus { get; set; } // ACTIVO, CANCELADO
            public string Comentario { get; set; }
        }

        private List<MovimientoMock> _historialCompleto;
        private string _filtroInicial;

        // Constructor que recibe el filtro del Menú Principal
        // Si no se pasa nada, asume "TODOS"
        public MovimientosView(string filtro = "TODOS")
        {
            InitializeComponent();
            _filtroInicial = filtro;

            ConfigurarVista();
            CargarDatosPrueba();
        }

        private void ConfigurarVista()
        {
            // Ajustar título y combo según el filtro recibido
            switch (_filtroInicial)
            {
                case "ENTR":
                    txtTitulo.Text = "Entradas de Mercancía";
                    pnlFiltroTipo.Visibility = Visibility.Collapsed; // Ocultamos filtro porque ya es fijo
                    break;
                case "SAL":
                    txtTitulo.Text = "Salidas de Almacén";
                    pnlFiltroTipo.Visibility = Visibility.Collapsed;
                    break;
                default:
                    txtTitulo.Text = "Historial Completo";
                    pnlFiltroTipo.Visibility = Visibility.Visible;
                    break;
            }

            // Fechas por defecto (Mes actual)
            dpDesde.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpHasta.SelectedDate = DateTime.Now;
        }

        private void CargarDatosPrueba()
        {
            // Simulamos datos de BD
            _historialCompleto = new List<MovimientoMock>
            {
                new MovimientoMock { Id=1, Folio="ENTR-00001", Fecha=DateTime.Now.AddDays(-5), Tipo="ENTRADA", Usuario="admin", Estatus="ACTIVO", Comentario="Recepción inicial" },
                new MovimientoMock { Id=2, Folio="ENTR-00002", Fecha=DateTime.Now.AddDays(-4), Tipo="ENTRADA", Usuario="juan.perez", Estatus="ACTIVO", Comentario="Compra Lote A" },
                new MovimientoMock { Id=3, Folio="SAL-00001", Fecha=DateTime.Now.AddDays(-2), Tipo="SALIDA", Usuario="juan.perez", Estatus="ACTIVO", Comentario="Venta Mostrador" },
                new MovimientoMock { Id=4, Folio="TRIN-00001", Fecha=DateTime.Now.AddDays(-1), Tipo="TRANSFERENCIA", Usuario="admin", Estatus="ACTIVO", Comentario="Reacomodo pasillo B" },
                new MovimientoMock { Id=5, Folio="SAL-00002", Fecha=DateTime.Now, Tipo="SALIDA", Usuario="admin", Estatus="CANCELADO", Comentario="Error de captura" }
            };

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            if (_historialCompleto == null) return;

            var query = _historialCompleto.AsEnumerable();

            // 1. Filtro Maestro (Viene del Menú)
            if (_filtroInicial == "ENTR") query = query.Where(x => x.Tipo == "ENTRADA");
            if (_filtroInicial == "SAL") query = query.Where(x => x.Tipo == "SALIDA");

            // 2. Filtro de Combo (Solo si estamos en vista TODOS)
            if (_filtroInicial == "TODOS" && cmbFiltroTipo.SelectedItem != null)
            {
                var item = (ComboBoxItem)cmbFiltroTipo.SelectedItem;
                if (item.Tag != null) // Ignorar "Todos"
                {
                    string tipoSeleccionado = item.Tag.ToString(); // ENTR, SAL, etc.
                    // Mapeo simple para el Mock
                    if (tipoSeleccionado == "ENTR") query = query.Where(x => x.Tipo == "ENTRADA");
                    if (tipoSeleccionado == "SAL") query = query.Where(x => x.Tipo == "SALIDA");
                    if (tipoSeleccionado == "TRANS") query = query.Where(x => x.Tipo == "TRANSFERENCIA");
                }
            }

            // 3. Filtro Texto (Folio)
            if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                query = query.Where(x => x.Folio.Contains(txtBuscar.Text.ToUpper()));
            }

            dgMovimientos.ItemsSource = query.ToList();
        }

        // Eventos de botones
        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void btnNuevoMovimiento_Click(object sender, RoutedEventArgs e)
        {
            // ABRIMOS LA VENTANA MODAL QUE CREAMOS ANTES
            RegistrarMovimientoWindow ventana = new RegistrarMovimientoWindow();

            // ShowDialog bloquea la ventana de atrás hasta que cierres la nueva
            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                MessageBox.Show("Movimiento registrado con éxito. Refrescando lista...");
                // Aquí llamaríamos a CargarDatosPrueba() o recargaríamos desde BD
                CargarDatosPrueba();
            }
        }

        private void btnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            // Obtenemos el objeto de la fila seleccionada
            var movimiento = ((Button)sender).DataContext as MovimientoMock;

            MessageBox.Show($"Aquí abriríamos el detalle del folio: {movimiento.Folio}\nProductos: [Lista Pendiente]");
        }
    }
}