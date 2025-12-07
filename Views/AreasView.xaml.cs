using GestorAlmacen.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
// Agrega: using System.Data.Entity; si necesitas .Include, pero aqui usaremos carga lazy o directa

namespace GestorAlmacen.Views
{
    public partial class AreasView : UserControl
    {
        private Area _seleccionado;

        public AreasView()
        {
            InitializeComponent();
            CargarListas(); // Cargar combo
            CargarDatos();  // Cargar grid
        }

        private void CargarListas()
        {
            using (var db = new WMS_DBEntities())
            {
                var cats = db.Categories.Where(c => c.is_active == true).ToList();
                // Agregamos opción vacía al principio si quieres, o manejamos null
                cmbCategoria.ItemsSource = cats;
            }
        }

        private void CargarDatos()
        {
            using (var db = new WMS_DBEntities())
            {
                // Incluimos la relación con Categories para mostrar el nombre en el Grid
                // Nota: Asegúrate que tu Grid en XAML haga Binding a 'Categories.name' en la columna correspondiente
                var lista = db.Areas.Include("Category").Where(a => a.is_active == true).ToList();
                dgAreas.ItemsSource = lista;
            }
        }

        // IMPORTANTE: En tu XAML (AreasView.xaml), busca la columna de 'Cat. Preferente' 
        // y cambia el Binding="{Binding CategoriaPreferente}" por Binding="{Binding Categories.name}"

        private void dgAreas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAreas.SelectedItem is Area area)
            {
                _seleccionado = area;
                txtCodigo.Text = area.code;
                txtCodigo.IsEnabled = false; // Código inmutable
                txtNombre.Text = area.name;
                txtCapacidad.Text = area.capacity.ToString();
                chkActivo.IsChecked = area.is_active;

                if (area.preferred_category_id != null)
                    cmbCategoria.SelectedValue = area.preferred_category_id;
                else
                    cmbCategoria.SelectedIndex = -1;

                btnGuardar.Content = "Actualizar";
                btnEliminar.Visibility = Visibility.Visible;
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            Limpiar();
        }

        private void Limpiar()
        {
            _seleccionado = null;
            txtCodigo.Text = "";
            txtCodigo.IsEnabled = true;
            txtNombre.Text = "";
            txtCapacidad.Text = "";
            cmbCategoria.SelectedIndex = -1;
            chkActivo.IsChecked = true;
            dgAreas.SelectedItem = null;
            btnGuardar.Content = "Guardar";
            btnEliminar.Visibility = Visibility.Collapsed;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text)) return;

            using (var db = new WMS_DBEntities())
            {
                int? catId = cmbCategoria.SelectedValue as int?;
                int.TryParse(txtCapacidad.Text, out int cap);

                if (_seleccionado == null)
                {
                    var nueva = new Area
                    {
                        code = txtCodigo.Text.Trim().ToUpper(),
                        name = txtNombre.Text.Trim(),
                        capacity = cap > 0 ? (int?)cap : null,
                        preferred_category_id = catId,
                        is_active = true,
                        created_at = DateTime.Now
                    };
                    db.Areas.Add(nueva);
                }
                else
                {
                    var edit = db.Areas.Find(_seleccionado.area_id);
                    edit.name = txtNombre.Text;
                    edit.capacity = cap > 0 ? (int?)cap : null;
                    edit.preferred_category_id = catId;
                    edit.is_active = (bool)chkActivo.IsChecked;
                }

                try
                {
                    db.SaveChanges();
                    Limpiar();
                    CargarDatos();
                }
                catch (Exception ex) { MessageBox.Show("Error (posible código duplicado): " + ex.Message); }
            }
        }

        // ... (Implementar btnEliminar y txtBuscar igual que en CategoriasView)
        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null) return;
            if (MessageBox.Show("¿Desactivar Área?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new WMS_DBEntities())
                {
                    var a = db.Areas.Find(_seleccionado.area_id);
                    a.is_active = false;
                    db.SaveChanges();
                    Limpiar();
                    CargarDatos();
                }
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica de filtro opcional
        }
    }
}