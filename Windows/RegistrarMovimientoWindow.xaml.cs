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
            string tipo = ((ComboBoxItem)cmbTipoMovimiento.SelectedItem).Tag.ToString(); // ENTR, SAL

            using (var db = new WMS_DBEntities())
            {
                using (var transaction = db.Database.Connection.BeginTransaction())
                {
                    try
                    {
                        // 1. OBTENER FOLIO (Llamada cruda a SP)
                        // EF no siempre mapea params output facil, usamos SQL raw
                        var folioParam = new System.Data.SqlClient.SqlParameter
                        {
                            ParameterName = "@folio",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 30,
                            Direction = System.Data.ParameterDirection.Output
                        };

                        db.Database.ExecuteSqlCommand("EXEC GetNextFolio @seq_name, @prefix, @folio OUT",
                            new System.Data.SqlClient.SqlParameter("@seq_name", tipo),
                            new System.Data.SqlClient.SqlParameter("@prefix", tipo),
                            folioParam);

                        string folioGenerado = folioParam.Value.ToString();

                        // 2. CREAR MOVIMIENTO CABECERA
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
                        db.SaveChanges(); // Para obtener mov.movement_id

                        // 3. DETALLES Y STOCK
                        foreach (var item in _carrito)
                        {
                            // A) Guardar Detalle
                            // Nota: Lógica simplificada. Si es Transferencia, se requieren 2 movimientos (Salida y Entrada)
                            // Para este ejemplo asumimos Entrada o Salida simple.

                            int areaIdFinal = (tipo == "ENTR") ? item.AreaDestinoId.Value : item.AreaOrigenId.Value;

                            var det = new MovementDetail
                            {
                                movement_id = mov.movement_id,
                                product_id = item.ProductId,
                                area_id = areaIdFinal,
                                quantity = item.Cantidad
                            };
                            db.MovementDetails.Add(det);

                            // B) Actualizar Stock
                            var stock = db.Stocks.FirstOrDefault(s => s.product_id == item.ProductId && s.area_id == areaIdFinal);
                            if (stock == null)
                            {
                                stock = new Stock { product_id = item.ProductId, area_id = areaIdFinal, quantity = 0 };
                                db.Stocks.Add(stock);
                            }

                            if (tipo == "ENTR") stock.quantity += item.Cantidad;
                            else stock.quantity -= item.Cantidad;

                            if (stock.quantity < 0) throw new Exception($"Stock insuficiente de {item.Sku}");
                        }

                        db.SaveChanges();
                        transaction.Commit();
                        MessageBox.Show($"Movimiento registrado: {folioGenerado}");
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Error en transacción: " + ex.Message);
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