using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;
using GestorAlmacen.Windows;

namespace GestorAlmacen.Views
{
    public partial class MovimientosView : UserControl
    {
       
        private string _filtroInicial;

        public MovimientosView(string filtro = "TODOS")
        {
            InitializeComponent();
            _filtroInicial = filtro; // Guardamos el contexto

            ConfigurarVista();
            CargarMovimientos();
        }

        private void ConfigurarVista()
        {
            switch (_filtroInicial)
            {
                case "ENTR":
                    txtTitulo.Text = "Entradas de Mercancía";
                    pnlFiltroTipo.Visibility = Visibility.Collapsed;
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

            // Filtro por defecto: Últimos 30 días
            dpDesde.SelectedDate = DateTime.Now.AddDays(-30);
            dpHasta.SelectedDate = DateTime.Now;
        }

        private void CargarMovimientos()
        {
            using (var db = new WMS_DBEntities())
            {
                var query = db.Movements.Include("User").AsQueryable();

                // Filtros de Fecha
                if (dpDesde.SelectedDate.HasValue)
                {
                    var desde = dpDesde.SelectedDate.Value.Date;
                    query = query.Where(m => m.movement_date >= desde);
                }

                if (dpHasta.SelectedDate.HasValue)
                {
                    var hasta = dpHasta.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(m => m.movement_date <= hasta);
                }

                // Filtro Maestro (Desde Menú Principal: ENTR o SAL)
                if (_filtroInicial == "ENTR") query = query.Where(x => x.movement_type == "ENTRADA");
                if (_filtroInicial == "SAL") query = query.Where(x => x.movement_type == "SALIDA");

                // Filtro de Combo (Solo visible si estamos en modo TODOS)
                if (_filtroInicial == "TODOS" && cmbFiltroTipo.SelectedItem != null)
                {
                    var item = (ComboBoxItem)cmbFiltroTipo.SelectedItem;
                    if (item.Tag != null)
                    {
                        string tipoTag = item.Tag.ToString();

                        if (tipoTag == "ENTR") query = query.Where(x => x.movement_type == "ENTRADA");
                        if (tipoTag == "SAL") query = query.Where(x => x.movement_type == "SALIDA");
                        if (tipoTag == "TRANS") query = query.Where(x => x.movement_type == "TRANSFERENCIA");
                    }
                }

                // Filtro Texto (Folio)
                if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    query = query.Where(x => x.folio.Contains(txtBuscar.Text));
                }

                // PROYECCIÓN (Select)
                var resultado = query
                    .OrderByDescending(m => m.movement_date)
                    .Select(m => new
                    {
                        // Definimos "Id". Esto es lo que buscará el botón ver detalle.
                        Id = m.movement_id,
                        Folio = m.folio,
                        Fecha = m.movement_date,
                        Tipo = m.movement_type,
                        Usuario = m.User != null ? m.User.username : "Sistema",
                        Estatus = m.status,
                        Comentario = m.comment
                    })
                    .ToList();

                dgMovimientos.ItemsSource = resultado;
            }
        }

        // --- Eventos ---

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            CargarMovimientos();
        }

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) CargarMovimientos();
        }

        private void btnNuevoMovimiento_Click(object sender, RoutedEventArgs e)
        {
            var win = new Windows.RegistrarMovimientoWindow(_filtroInicial);

            if (win.ShowDialog() == true)
            {
                CargarMovimientos();
            }
        }

        private void btnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            var dataRow = boton.DataContext;

            try
            {
                // Para clase Movement de EF directamente
                if (dataRow is Movement movEntity)
                {
                    new GestorAlmacen.Windows.DetalleMovimientoWindow(movEntity.movement_id).ShowDialog();
                    return;
                }

                // Para objeto anónimo. Buscamos la propiedad "Id" que definimos en CargarMovimientos
                var propId = dataRow.GetType().GetProperty("Id");
                if (propId != null)
                {
                    int id = (int)propId.GetValue(dataRow, null);
                    new GestorAlmacen.Windows.DetalleMovimientoWindow(id).ShowDialog();
                }
                else
                {
                    // Respaldo por si se llama movement_id
                    var propMovId = dataRow.GetType().GetProperty("movement_id");
                    if (propMovId != null)
                    {
                        int id = (int)propMovId.GetValue(dataRow, null);
                        new GestorAlmacen.Windows.DetalleMovimientoWindow(id).ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir detalle: " + ex.Message);
            }
        }
    }
}