using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestorAlmacen.Views
{
    public partial class AreasView : UserControl
    {
        // Mock de datos
        public class AreaMock
        {
            public int Id { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public int? Capacidad { get; set; }
            public string CategoriaPreferente { get; set; }
            public bool IsActive { get; set; }
        }

        public class CategoriaSimpleMock { public int Id { get; set; } public string Nombre { get; set; } }

        private List<AreaMock> _listaAreas;
        private AreaMock _seleccionado; // Para saber si estamos editando

        public AreasView()
        {
            InitializeComponent();
            CargarCategorias();
            CargarDatosPrueba();
        }

        private void CargarDatosPrueba()
        {
            _listaAreas = new List<AreaMock>
            {
                new AreaMock { Id=1, Codigo="A1", Nombre="Pasillo A - Sección 1", Capacidad=100, CategoriaPreferente="Celulares", IsActive=true },
                new AreaMock { Id=2, Codigo="A2", Nombre="Pasillo A - Sección 2", Capacidad=100, CategoriaPreferente="Celulares", IsActive=true },
                new AreaMock { Id=3, Codigo="B1", Nombre="Zona Carga Pesada", Capacidad=50, CategoriaPreferente="Línea Blanca", IsActive=true },
                new AreaMock { Id=4, Codigo="Z9", Nombre="Área Cuarentena", Capacidad=null, CategoriaPreferente="Sin Asignar", IsActive=false }
            };
            dgAreas.ItemsSource = _listaAreas;
        }

        private void CargarCategorias()
        {
            // Llenar combo para 'Preferred Category'
            var cats = new List<CategoriaSimpleMock>
            {
                new CategoriaSimpleMock { Id=0, Nombre="-- Ninguna --" },
                new CategoriaSimpleMock { Id=1, Nombre="Celulares" },
                new CategoriaSimpleMock { Id=2, Nombre="Línea Blanca" },
                new CategoriaSimpleMock { Id=3, Nombre="Cómputo" }
            };
            cmbCategoria.ItemsSource = cats;
            cmbCategoria.SelectedIndex = 0;
        }

        // --- Eventos de UI ---

        private void dgAreas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAreas.SelectedItem is AreaMock area)
            {
                // MODO EDICIÓN: Pasar datos al formulario
                _seleccionado = area;

                txtCodigo.Text = area.Codigo;
                txtCodigo.IsEnabled = false; // El código no suele editarse por integridad
                txtNombre.Text = area.Nombre;
                txtCapacidad.Text = area.Capacidad?.ToString() ?? "";
                chkActivo.IsChecked = area.IsActive;

                // Seleccionar la categoría en el combo (Lógica simple por nombre para el Mock)
                foreach (CategoriaSimpleMock item in cmbCategoria.Items)
                {
                    if (item.Nombre == area.CategoriaPreferente)
                    {
                        cmbCategoria.SelectedItem = item;
                        break;
                    }
                }

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
            txtCodigo.Text = "";
            txtCodigo.IsEnabled = true;
            txtNombre.Text = "";
            txtCapacidad.Text = "";
            cmbCategoria.SelectedIndex = 0;
            chkActivo.IsChecked = true;

            dgAreas.SelectedItem = null;
            btnGuardar.Content = "Guardar";
            btnEliminar.Visibility = Visibility.Collapsed;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("El código es obligatorio.");
                return;
            }

            // Aquí iría la lógica INSERT o UPDATE a SQL Server
            string mensaje = (_seleccionado == null) ? "Área Creada" : "Área Actualizada";
            MessageBox.Show($"{mensaje} correctamente.\n(Simulado)");

            LimpiarFormulario();
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado != null)
            {
                MessageBox.Show($"El área {_seleccionado.Codigo} ha sido inhabilitada (Soft Delete).");
                // _seleccionado.IsActive = false; // Guardar en BD
                LimpiarFormulario();
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Filtro simple
            string query = txtBuscar.Text.ToLower();
            if (_listaAreas != null)
            {
                var filtrado = _listaAreas.Where(x => x.Codigo.ToLower().Contains(query) || x.Nombre.ToLower().Contains(query)).ToList();
                dgAreas.ItemsSource = filtrado;
            }
        }
    }
}
