using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;
using System.Data.Entity;

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

        public RegistrarMovimientoWindow()
        {
            InitializeComponent();
            lblFecha.Text = DateTime.Now.ToShortDateString();
            _carrito = new ObservableCollection<DetalleItem>();
            dgDetalles.ItemsSource = _carrito;
            CargarAreas();
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

        // ... MANTENER LÓGICA DE VISIBILIDAD DE PANELES IGUAL QUE ANTES ...
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
            // Validaciones (Cantidad, selección de áreas...)
            if (lblNombreProducto.Tag == null) return;
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

            // Obtener Tag (ENTR o SAL)
            if (cmbTipoMovimiento.SelectedItem == null) return;
            string tipo = ((ComboBoxItem)cmbTipoMovimiento.SelectedItem).Tag.ToString();

            using (var db = new WMS_DBEntities())
            {
                // 1. ABRIR CONEXIÓN MANUALMENTE
                // Necesario porque vamos a usar una transacción manual clásica
                if (db.Database.Connection.State != System.Data.ConnectionState.Open)
                {
                    db.Database.Connection.Open();
                }

                // Usamos .Connection.BeginTransaction() (La forma clásica compatible con tu versión)
                using (var transaction = db.Database.Connection.BeginTransaction())
                {
                    try
                    {
                        // ----------------------------------------------------------------
                        // PASO A: EJECUTAR EL STORED PROCEDURE MANUALMENTE
                        // ----------------------------------------------------------------
                        var cmd = db.Database.Connection.CreateCommand();

                        // ASIGNACIÓN DIRECTA DE TRANSACCIÓN:
                        // Como 'transaction' ya es el objeto real, lo asignamos directo.
                        // (Ya no usamos .UnderlyingTransaction porque no existe en tu versión)
                        cmd.Transaction = transaction;

                        cmd.CommandText = "GetNextFolio";
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        // Parámetros
                        var pSeq = cmd.CreateParameter();
                        pSeq.ParameterName = "@seq_name";
                        pSeq.Value = tipo;
                        cmd.Parameters.Add(pSeq);

                        var pPrefix = cmd.CreateParameter();
                        pPrefix.ParameterName = "@prefix";
                        pPrefix.Value = tipo;
                        cmd.Parameters.Add(pPrefix);

                        // CORRECCIÓN DE NOMBRE: @folio_generado (Según tu error anterior)
                        var pFolio = cmd.CreateParameter();
                        pFolio.ParameterName = "@folio_generado";
                        pFolio.Direction = System.Data.ParameterDirection.Output;
                        pFolio.Size = 30;
                        cmd.Parameters.Add(pFolio);

                        // Ejecutamos el comando asociado a la transacción
                        cmd.ExecuteNonQuery();

                        string folioGenerado = pFolio.Value.ToString();

                        // ----------------------------------------------------------------
                        // PASO B: GUARDAR EN ENTITY FRAMEWORK
                        // ----------------------------------------------------------------

                        // 2. Cabecera del Movimiento
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

                        // EF detectará que la conexión está abierta y la usará,
                        // respetando la transacción que iniciaste en esa conexión.
                        db.SaveChanges();

                        // 3. Detalles y Stock
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
                                stock = new Stock { product_id = item.ProductId, area_id = areaIdFinal, quantity = 0 };
                                db.Stocks.Add(stock);
                            }

                            if (tipo == "ENTR") stock.quantity += item.Cantidad;
                            else stock.quantity -= item.Cantidad;

                            if (stock.quantity < 0) throw new Exception($"Stock insuficiente para el producto {item.Sku}");
                        }

                        db.SaveChanges();

                        // 4. CONFIRMAR TODO
                        transaction.Commit();

                        MessageBox.Show($"Movimiento registrado correctamente: {folioGenerado}");
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        // Rollback defensivo
                        try { transaction.Rollback(); } catch { }

                        string msg = ex.Message;
                        if (ex.InnerException != null) msg += "\nDetalle: " + ex.InnerException.Message;

                        MessageBox.Show("Error al guardar: " + msg, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        


        // ... Otros métodos auxiliares (QuitarFila, etc) ...
        private void btnQuitarFila_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ActualizarTotales() { txtTotalItems.Text = _carrito.Sum(x => x.Cantidad).ToString(); }
        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}