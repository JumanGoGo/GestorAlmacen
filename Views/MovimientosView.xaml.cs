using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;
using GestorAlmacen.Views.Windows;

namespace GestorAlmacen.Views
{
    public partial class MovimientosView : UserControl
    {
        private string _filtroInicial;

        public MovimientosView(string filtro = "TODOS")
        {
            InitializeComponent();
            _filtroInicial = filtro;
            ConfigurarVista();
            CargarMovimientos(); // Renombrado para reflejar que es real
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
                // 1. Iniciamos la consulta base
                var query = db.Movements.Include("User").AsQueryable();

                // 2. Aplicar Filtros de Fecha
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

                // 3. Filtro Maestro (Desde Menú Principal)
                // CORRECCIÓN: Usamos 'movement_type' en lugar de 'type'
                if (_filtroInicial == "ENTR") query = query.Where(x => x.movement_type == "ENTRADA");
                if (_filtroInicial == "SAL") query = query.Where(x => x.movement_type == "SALIDA");

                // 4. Filtro de Combo
                if (_filtroInicial == "TODOS" && cmbFiltroTipo.SelectedItem != null)
                {
                    var item = (ComboBoxItem)cmbFiltroTipo.SelectedItem;
                    if (item.Tag != null)
                    {
                        string tipoTag = item.Tag.ToString();

                        // CORRECCIÓN: Usamos 'movement_type'
                        if (tipoTag == "ENTR") query = query.Where(x => x.movement_type == "ENTRADA");
                        if (tipoTag == "SAL") query = query.Where(x => x.movement_type == "SALIDA");
                        if (tipoTag == "TRANS") query = query.Where(x => x.movement_type == "TRANSFERENCIA");
                    }
                }

                // 5. Filtro Texto (Folio)
                if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    query = query.Where(x => x.folio.Contains(txtBuscar.Text));
                }

                // 6. PROYECCIÓN (Select) - Adaptando nombres de BD a Vista
                var resultado = query
                    .OrderByDescending(m => m.movement_date)
                    .Select(m => new
                    {
                        Id = m.movement_id,
                        Folio = m.folio,
                        Fecha = m.movement_date,

                        // CORRECCIÓN: 'movement_type' en lugar de 'type'
                        Tipo = m.movement_type,

                        // NOTA: Asumo que tu tabla Users tiene un campo 'username'. 
                        // Si te marca error aquí, cámbialo por el nombre correcto (ej. m.User.Nombre)
                        Usuario = m.User != null ? m.User.username : "Sistema",

                        Estatus = m.status,

                        // CORRECCIÓN: 'comment' (singular) en lugar de 'comments'
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
            // Evitamos recargar al iniciar si no está listo
            if (this.IsLoaded) CargarMovimientos();
        }

        private void btnNuevoMovimiento_Click(object sender, RoutedEventArgs e)
        {
            RegistrarMovimientoWindow ventana = new RegistrarMovimientoWindow();
            if (ventana.ShowDialog() == true)
            {
                CargarMovimientos(); // Recargamos la BD real
            }
        }

        private void btnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            // Usamos 'dynamic' porque estamos usando una clase anónima en el Select
            dynamic movimiento = ((Button)sender).DataContext;

            // Aquí podrías abrir una ventana pasando el ID real
            MessageBox.Show($"Abriendo detalle para ID: {movimiento.Id} - Folio: {movimiento.Folio}");
        }
    }
}