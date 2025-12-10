using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;
using System.Data.Entity;
using System.Transactions;

namespace GestorAlmacen.Views.Windows
{
    public partial class RegistrarMovimientoWindow : Window
    {
        public class DetalleItem
        {
            public int ProductId { get; set; }
            public string Sku { get; set; }
            public string NombreProducto { get; set; }
            public int? AreaOrigenId { get; set; }
            public string AreaOrigenNombre { get; set; }
            public int? AreaDestinoId { get; set; }
            public string AreaDestinoNombre { get; set; }
            public int Cantidad { get; set; }
        }

        private ObservableCollection<DetalleItem> _carrito;
        private int _usuarioId = 1; // DEBES OBTENER ESTO DEL LOGIN (Pasarlo en constructor)

        // Constructor modificado
        public RegistrarMovimientoWindow(string restriccion = null)
        {
            InitializeComponent();
            lblFecha.Text = DateTime.Now.ToShortDateString();

            _carrito = new ObservableCollection<DetalleItem>();
            dgDetalles.ItemsSource = _carrito;
            CargarAreas();

            // Llamamos a la nueva lógica de filtrado
            ConfigurarCombo(restriccion);
        }

        private void ConfigurarCombo(string restriccion)
        {
            // Si no hay restricción, no hacemos nada (se muestran todos)
            if (string.IsNullOrEmpty(restriccion)) return;

            var itemsAEliminar = new System.Collections.Generic.List<object>();

            foreach (ComboBoxItem item in cmbTipoMovimiento.Items)
            {
                string tag = item.Tag.ToString();

                // Si estamos en modo "SAL" (Salidas), quitamos las Entradas (ENTR)
                if (restriccion == "SAL" && tag == "ENTR")
                {
                    itemsAEliminar.Add(item);
                }
                // Si estuviéramos en modo "ENTR" (Entradas), quitamos Salidas y Transferencias
                else if (restriccion == "ENTR" && (tag == "SAL" || tag == "TRANS"))
                {
                    itemsAEliminar.Add(item);
                }
            }

            // Eliminamos los ítems detectados
            foreach (var item in itemsAEliminar)
            {
                cmbTipoMovimiento.Items.Remove(item);
            }

            // Aseguramos que siempre haya algo seleccionado
            if (cmbTipoMovimiento.Items.Count > 0)
            {
                cmbTipoMovimiento.SelectedIndex = 0;
            }
        }


        private void CargarAreas()
        {
            using (var db = new WMS_DBEntities())
            {
                var areas = db.Areas.Where(a => a.is_active == true).ToList();
                cmbAreaOrigen.ItemsSource = areas; // DisplayMemberPath="code" en XAML
                cmbAreaDestino.ItemsSource = areas;
            }
        }


        private void cmbTipoMovimiento_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { /* Copiar lógica previa de visibilidad */ }

        private void btnBuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new WMS_DBEntities())
            {
                var prod = db.Products.FirstOrDefault(p => p.sku == txtSkuBuscar.Text);
                if (prod != null)
                {
                    lblNombreProducto.Text = prod.name;
                    lblNombreProducto.Tag = prod.product_id; // Guardamos ID oculto
                }
                else MessageBox.Show("Producto no encontrado.");
            }
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones

            int.TryParse(txtCantidad.Text, out int cant);
            if (cant <= 0) return;

            var item = new DetalleItem
            {
                ProductId = (int)lblNombreProducto.Tag,
                Sku = txtSkuBuscar.Text,
                NombreProducto = lblNombreProducto.Text,
                Cantidad = cant
            };

            // Mapear Áreas (IDs)
            if (pnlOrigen.Visibility == Visibility.Visible && cmbAreaOrigen.SelectedItem is Area origen)
            {
                item.AreaOrigenId = origen.area_id;
                item.AreaOrigenNombre = origen.code;
            }
            if (pnlDestino.Visibility == Visibility.Visible && cmbAreaDestino.SelectedItem is Area destino)
            {
                item.AreaDestinoId = destino.area_id;
                item.AreaDestinoNombre = destino.code;
            }

            _carrito.Add(item);
            ActualizarTotales();
        }
        private void btnGuardarTodo_Click(object sender, RoutedEventArgs e)
        {
            if (_carrito.Count == 0) return;
            if (cmbTipoMovimiento.SelectedItem == null) return;

            string tipo = ((ComboBoxItem)cmbTipoMovimiento.SelectedItem).Tag.ToString();

            //TransactionScope: Maneja la transacción automáticamente para EF y SQL puro
            using (var scope = new TransactionScope())
            {
                using (var db = new WMS_DBEntities())
                {
                    try
                    {
                        //EJECUTAR STORED PROCEDURE
      

                        // Aseguramos conexión abierta
                        if (db.Database.Connection.State != System.Data.ConnectionState.Open)
                            db.Database.Connection.Open();

                        var cmd = db.Database.Connection.CreateCommand();
                        cmd.CommandText = "GetNextFolio";
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        // Parámetros
                        var pSeq = cmd.CreateParameter();
                        pSeq.ParameterName = "@seq_name"; pSeq.Value = tipo;
                        cmd.Parameters.Add(pSeq);

                        var pPrefix = cmd.CreateParameter();
                        pPrefix.ParameterName = "@prefix"; pPrefix.Value = tipo;
                        cmd.Parameters.Add(pPrefix);

                        // Parámetro de salida correcto
                        var pFolio = cmd.CreateParameter();
                        pFolio.ParameterName = "@folio_generado";
                        pFolio.Direction = System.Data.ParameterDirection.Output;
                        pFolio.Size = 30;
                        cmd.Parameters.Add(pFolio);

                        cmd.ExecuteNonQuery(); // Ejecutamos

                        string folioGenerado = pFolio.Value.ToString();

                        // GUARDAR EN ENTITY FRAMEWORK
                        // EF se unirá automáticamente a la transacción del TransactionScope

                        var mov = new Movement
                        {
                            folio = folioGenerado,
                            movement_type = tipo == "ENTR" ? "ENTRADA" : "SALIDA",
                            status = "ACTIVE",
                            movement_date = DateTime.Now,
                            user_id = _usuarioId,
                            comment = txtComentario.Text
                        };
                        db.Movements.Add(mov);
                        db.SaveChanges(); // Guardamos cabecera

                        // DETALLES
                        foreach (var item in _carrito)
                        {
                            int areaIdFinal = (tipo == "ENTR") ? item.AreaDestinoId.Value : item.AreaOrigenId.Value;

                            var det = new MovementDetail
                            {
                                movement_id = mov.movement_id,
                                product_id = item.ProductId,
                                area_id = areaIdFinal,
                                quantity = item.Cantidad

                            };
                            db.MovementDetails.Add(det);

                            // Stock
                            var stock = db.Stocks.FirstOrDefault(s => s.product_id == item.ProductId && s.area_id == areaIdFinal);
                            if (stock == null)
                            {
                                stock = new Stock { product_id = item.ProductId, area_id = areaIdFinal, quantity = 0, last_update = DateTime.Now};
                                db.Stocks.Add(stock);
                            }

                            if (tipo == "ENTR") stock.quantity += item.Cantidad;
                            else stock.quantity -= item.Cantidad;

                            stock.last_update = DateTime.Now;

                            if (stock.quantity < 0) throw new Exception($"Stock insuficiente: {item.Sku}");
                        }

                        db.SaveChanges(); // Guardamos detalles y stock

                        // CONFIRMAR TRANSACCIÓN
                        scope.Complete(); // EQUIVALENTE A COMMIT

                        MessageBox.Show($"Movimiento registrado: {folioGenerado}");
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        // En TransactionScope, si no llamamos a scope.Complete(), 
                        // el Rollback es automático al salir del 'using'.

                        string msg = ex.Message;
                        if (ex.InnerException != null) msg += "\nDetalle: " + ex.InnerException.Message;
                        MessageBox.Show("Error al guardar: " + msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }



        // Métodos auxiliares
        private void btnQuitarFila_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ActualizarTotales() { txtTotalItems.Text = _carrito.Sum(x => x.Cantidad).ToString(); }
        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}