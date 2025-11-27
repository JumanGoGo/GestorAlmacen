using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestorAlmacen.Views
{
    public partial class CategoriasView : UserControl
    {
        // Mock de datos
        public class CategoriaMock
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public bool IsActive { get; set; }
        }

        private List<CategoriaMock> _listaCategorias;
        private CategoriaMock _seleccionado;

        public CategoriasView()
        {
            InitializeComponent();
            CargarDatosPrueba();
        }

        private void CargarDatosPrueba()
        {
            // Datos iniciales como en tu script SQL
            _listaCategorias = new List<CategoriaMock>
            {
                new CategoriaMock { Id=1, Nombre="Celulares", Descripcion="Telefonía móvil", IsActive=true },
                new CategoriaMock { Id=2, Nombre="Televisiones", Descripcion="TVs y monitores", IsActive=true },
                new CategoriaMock { Id=3, Nombre="Línea Blanca", Descripcion="Electrodomésticos grandes", IsActive=true },
                new CategoriaMock { Id=4, Nombre="Audio/Bocinas", Descripcion="Equipo de audio", IsActive=true },
                new CategoriaMock { Id=5, Nombre="Cómputo", Descripcion="PC, laptops y accesorios", IsActive=true }
            };
            dgCategorias.ItemsSource = _listaCategorias;
        }

        // --- Eventos de UI ---

        private void dgCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCategorias.SelectedItem is CategoriaMock cat)
            {
                _seleccionado = cat;

                txtNombre.Text = cat.Nombre;
                txtDescripcion.Text = cat.Descripcion;
                chkActivo.IsChecked = cat.IsActive;

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
                MessageBox.Show("El nombre de la categoría es obligatorio.");
                return;
            }

            // Aquí iría el INSERT o UPDATE a la tabla Categories
            if (_seleccionado == null)
            {
                MessageBox.Show($"Categoría '{txtNombre.Text}' CREADA correctamente.");
            }
            else
            {
                MessageBox.Show($"Categoría '{txtNombre.Text}' ACTUALIZADA correctamente.");
            }

            LimpiarFormulario();
            // Recargar lista...
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado != null)
            {
                var result = MessageBox.Show($"¿Seguro que desea desactivar '{_seleccionado.Nombre}'?\nEsto puede afectar productos activos.",
                                             "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Categoría desactivada (Soft Delete).");
                    LimpiarFormulario();
                }
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = txtBuscar.Text.ToLower();
            if (_listaCategorias != null)
            {
                var filtrado = _listaCategorias.Where(x => x.Nombre.ToLower().Contains(query)).ToList();
                dgCategorias.ItemsSource = filtrado;
            }
        }
    }
}
