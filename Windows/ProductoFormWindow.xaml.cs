using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using GestorAlmacen.Models;

namespace GestorAlmacen.Views.Windows
{
    public partial class ProductoFormWindow : Window
    {
        private int? _productoId; // Si es null, es nuevo. Si tiene valor, es edición.

        public ProductoFormWindow(int? idProducto)
        {
            InitializeComponent();
            _productoId = idProducto;
            CargarCombos();

            if (_productoId.HasValue)
                CargarDatosProducto(_productoId.Value);
        }

        private void CargarCombos()
        {
            using (var db = new WMS_DBEntities())
            {
                cmbCategoria.ItemsSource = db.Categories.Where(c => c.is_active == true).ToList();
            }
        }

        private void CargarDatosProducto(int id)
        {
            using (var db = new WMS_DBEntities())
            {
                var prod = db.Products.Find(id);
                if (prod != null)
                {
                    txtSku.Text = prod.sku;
                    txtSku.IsEnabled = false; // SKU no editable
                    txtNombre.Text = prod.name;
                    cmbCategoria.SelectedValue = prod.category_id;
                    chkActivo.IsChecked = prod.is_active;
                    // ... mapear otros campos
                }
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones Regex SKU ... (Mantener las que ya tenías)
            string skuPattern = @"^[A-Z]{3}-\d{3}$";
            if (!Regex.IsMatch(txtSku.Text, skuPattern))
            {
                MessageBox.Show("Formato SKU inválido."); return;
            }

            if (cmbCategoria.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar una categoría obligatoriamente.");
                return;
            }

            using (var db = new WMS_DBEntities())
            {
                if (!_productoId.HasValue) // NUEVO
                {
                    // Validar unicidad SKU
                    if (db.Products.Any(p => p.sku == txtSku.Text))
                    {
                        MessageBox.Show("El SKU ya existe."); return;
                    }

                    var nuevo = new Product
                    {
                        sku = txtSku.Text,
                        name = txtNombre.Text,
                        category_id = (int)cmbCategoria.SelectedValue,
                        is_active = (bool)chkActivo.IsChecked,
                        created_at = DateTime.Now
                    };
                    db.Products.Add(nuevo);
                }
                else // EDITAR
                {
                    var edit = db.Products.Find(_productoId.Value);
                    edit.name = txtNombre.Text;
                    edit.category_id = (int)cmbCategoria.SelectedValue;
                    edit.is_active = (bool)chkActivo.IsChecked;
                }

                db.SaveChanges();
                this.DialogResult = true; // Éxito
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}