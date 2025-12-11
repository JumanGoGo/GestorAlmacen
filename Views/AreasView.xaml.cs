using GestorAlmacen.Models;
using System;
using System.Linq; 
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace GestorAlmacen.Views
{
    public partial class AreasView : UserControl
    {
        private Area _seleccionado;

        public AreasView()
        {
            InitializeComponent();
            CargarListas();
            CargarDatos();
        }

      
        private void CargarListas()
        {
            using (var db = new WMS_DBEntities())
            {
                var cats = db.Categories.Where(c => c.is_active == true).ToList();
                cmbCategoria.ItemsSource = cats;
            }
        }

        private void CargarDatos()
        {
            using (var db = new WMS_DBEntities())
            {
                var lista = db.Areas
                      .Include("Category")
                      .OrderByDescending(a => a.is_active)
                      .ThenBy(a => a.code)
                      .ToList();
                dgAreas.ItemsSource = lista;
            }
        }

        private void dgAreas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAreas.SelectedItem is Area area)
            {
                _seleccionado = area;
                txtCodigo.Text = area.code;
                txtCodigo.IsEnabled = false;
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

        private void btnLimpiar_Click(object sender, RoutedEventArgs e) => Limpiar();

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
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("El código es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var db = new WMS_DBEntities())
            {
                int? catId = cmbCategoria.SelectedValue as int?;
                if (!int.TryParse(txtCapacidad.Text, out int cap) && !string.IsNullOrWhiteSpace(txtCapacidad.Text))
                {
                    MessageBox.Show("La capacidad debe ser un número entero válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                int? capacidadFinal = string.IsNullOrWhiteSpace(txtCapacidad.Text) ? (int?)null : cap;

                if (_seleccionado == null)
                {
                    // NUEVA ÁREA (INSERT)
                    var nueva = new Area
                    {
                        code = txtCodigo.Text.Trim().ToUpper(),
                        name = txtNombre.Text.Trim(),
                        capacity = capacidadFinal,
                        preferred_category_id = catId,
                        is_active = true, // Las nuevas nacen activas
                        created_at = DateTime.Now
                    };
                    db.Areas.Add(nueva);
                }
                else
                {
              
                    // VALIDACIÓN DE STOCK: Si el usuario desmarcó el checkbox de activo...
                    if (chkActivo.IsChecked == false)
                    {
                        // Verificamos si hay stock mayor a 0 en esta área
                        bool tieneMaterial = db.Stocks.Any(s => s.area_id == _seleccionado.area_id && s.quantity > 0);

                        if (tieneMaterial)
                        {
                            MessageBox.Show("No se puede desactivar esta área porque contiene material.\n\n" +
                                            "Por favor, realice una TRANSFERENCIA de todo el material a otra ubicación antes de desactivarla.",
                                            "Bloqueo de Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);

                            // Revertimos el checkbox visualmente para que el usuario vea que no se pudo
                            chkActivo.IsChecked = true;
                            return; // Cancelamos el guardado
                        }
                    }

                    var edit = db.Areas.Find(_seleccionado.area_id);
                    edit.name = txtNombre.Text;
                    edit.capacity = capacidadFinal;
                    edit.preferred_category_id = catId;
                    edit.is_active = (bool)chkActivo.IsChecked;
                }

                try
                {
                    db.SaveChanges();
                    Limpiar();
                    CargarDatos();
                    MessageBox.Show("Datos guardados correctamente.");
                }
                catch (Exception ex) { MessageBox.Show("Error (posible código duplicado): " + ex.Message); }
            }
        }

     
        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null) return;

            using (var db = new WMS_DBEntities())
            {
                // 1. VALIDACIÓN DE STOCK ANTES DE PREGUNTAR
                // Usamos .Any() que es muy rápido, devuelve true apenas encuentra 1 registro
                bool tieneMaterial = db.Stocks.Any(s => s.area_id == _seleccionado.area_id && s.quantity > 0);

                if (tieneMaterial)
                {
                    MessageBox.Show($"El área '{_seleccionado.code}' contiene existencias.\n\n" +
                                    "Acción requerida: Mueva el material a otra ubicación mediante una Transferencia.",
                                    "Imposible Desactivar", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return; // Detenemos el proceso aquí
                }

                // 2. Si no tiene stock, procedemos con la confirmación normal
                if (MessageBox.Show("¿Desactivar Área?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
            using (var db = new WMS_DBEntities())
            {
                string query = txtBuscar.Text.ToLower();
                var resultado = db.Areas
                                  .Include("Category")
                                  .Where(a => a.code.ToLower().Contains(query) ||
                                              a.name.ToLower().Contains(query))
                                  .OrderByDescending(a => a.is_active)
                                  .ThenBy(a => a.code)
                                  .ToList();
                dgAreas.ItemsSource = resultado;
            }
        }
    }
}