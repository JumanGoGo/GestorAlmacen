using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;
using GestorAlmacen.Views;

namespace GestorAlmacen.Views
{
    public partial class InventarioView : UserControl
    {
        public InventarioView()
        {
            InitializeComponent();
            CargarAreasCombo();
            CargarInventario();
        }

        private void CargarAreasCombo()
        {
            using (var db = new WMS_DBEntities())
            {
                var areas = db.Areas.Where(a => a.is_active == true).Select(a => a.code).ToList();
                foreach (var a in areas) cmbAreas.Items.Add(a);
            }
        }

        private void CargarInventario()
        {
            using (var db = new WMS_DBEntities())
            {
                // Unimos las tablas para mostrar info legible
                var query = from s in db.Stocks
                            join p in db.Products on s.product_id equals p.product_id
                            join a in db.Areas on s.area_id equals a.area_id
                            join c in db.Categories on p.category_id equals c.category_id
                            where s.quantity > 0 // Solo mostramos lo que existe
                            select new
                            {
                                CodigoArea = a.code,
                                Sku = p.sku,
                                NombreProducto = p.name,
                                Categoria = c.name,
                                Cantidad = s.quantity
                            };

                // Filtros en memoria (o podrías hacerlos en la query antes del ToList)
                var resultado = query.ToList();

                // 1. Filtro Texto
                string txt = txtBuscar.Text.ToLower();
                if (!string.IsNullOrEmpty(txt))
                    resultado = resultado.Where(x => x.Sku.ToLower().Contains(txt) || x.NombreProducto.ToLower().Contains(txt)).ToList();

                // 2. Filtro Área
                if (cmbAreas.SelectedIndex > 0) // 0 es "Todas"
                {
                    string area = cmbAreas.SelectedItem.ToString();
                    if (cmbAreas.SelectedItem is ComboBoxItem item) area = item.Content.ToString();

                    if (area != "Todas")
                        resultado = resultado.Where(x => x.CodigoArea == area).ToList();
                }

                //dgStock.ItemsSource = resultado;
                //txtTotalCantidad.Text = resultado.Sum(x => x.Cantidad).ToString();
            }
        }

        private void Filtros_Changed(object sender, RoutedEventArgs e) => CargarInventario();
        private void btnRefrescar_Click(object sender, RoutedEventArgs e) { txtBuscar.Text = ""; cmbAreas.SelectedIndex = 0; CargarInventario(); }
    }
}