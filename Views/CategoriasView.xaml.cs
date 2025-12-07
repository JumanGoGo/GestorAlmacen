using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models; // Namespace donde está tu EF .edmx

namespace GestorAlmacen.Views
{
    public partial class CategoriasView : UserControl
    {
        // Variable para guardar el objeto que estamos editando
        private Category _seleccionado;

        public CategoriasView()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                using (var db = new WMS_DBEntities())
                {
                    // Cargamos solo las categorías activas
                    var lista = db.Categories
                                  .Where(c => c.is_active == true)
                                  .OrderBy(c => c.name)
                                  .ToList();
                    dgCategorias.ItemsSource = lista;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message);
            }
        }

        private void dgCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCategorias.SelectedItem is Category cat)
            {
                _seleccionado = cat;
                txtNombre.Text = cat.name;
                txtDescripcion.Text = cat.description;
                chkActivo.IsChecked = cat.is_active;

                btnGuardar.Content = "Actualizar";
                btnEliminar.Visibility = Visibility.Visible;
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _seleccionado = null;
            txtNombre.Text = "";
            txtDescripcion.Text = "";
            chkActivo.IsChecked = true;
            dgCategorias.SelectedItem = null;
            btnGuardar.Content = "Guardar";
            btnEliminar.Visibility = Visibility.Collapsed;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            try
            {
                using (var db = new WMS_DBEntities())
                {
                    if (_seleccionado == null)
                    {
                        // INSERT
                        var nuevaCat = new Category
                        {
                            name = txtNombre.Text.Trim(),
                            description = txtDescripcion.Text.Trim(),
                            is_active = (bool)chkActivo.IsChecked,
                            created_at = DateTime.Now
                        };
                        db.Categories.Add(nuevaCat);
                    }
                    else
                    {
                        // UPDATE - Buscamos el registro fresco en BD para editarlo
                        var catEditar = db.Categories.Find(_seleccionado.category_id);
                        if (catEditar != null)
                        {
                            catEditar.name = txtNombre.Text.Trim();
                            catEditar.description = txtDescripcion.Text.Trim();
                            catEditar.is_active = (bool)chkActivo.IsChecked;
                        }
                    }

                    db.SaveChanges();
                    MessageBox.Show("Guardado correctamente.");
                    LimpiarFormulario();
                    CargarDatos(); // Recargar grilla
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null) return;

            if (MessageBox.Show("¿Desactivar categoría?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new WMS_DBEntities())
                {
                    var cat = db.Categories.Find(_seleccionado.category_id);
                    if (cat != null)
                    {
                        cat.is_active = false; // Soft Delete
                        db.SaveChanges();
                        LimpiarFormulario();
                        CargarDatos();
                    }
                }
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
        
            using (var db = new WMS_DBEntities())
            {
                var query = txtBuscar.Text.ToLower();
                dgCategorias.ItemsSource = db.Categories
                                             .Where(c => c.is_active == true && c.name.Contains(query))
                                             .ToList();
            }
        }
    }
}