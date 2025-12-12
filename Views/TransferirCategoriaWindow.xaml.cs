using GestorAlmacen.Models;
using System.Collections.Generic;
using System.Windows;

namespace GestorAlmacen.Views // Asegúrate de que el namespace sea correcto
{
    public partial class TransferirCategoriaWindow : Window
    {
        // Propiedad para devolver la ID de la categoría seleccionada
        public int? CategoriaDestinoId { get; private set; }

        // La ID de la categoría que estamos intentando desactivar (origen)
        private int _categoriaOrigenId;

        public TransferirCategoriaWindow(int categoriaOrigenId, string categoriaOrigenNombre, List<Category> categoriasActivas)
        {
            InitializeComponent();
            _categoriaOrigenId = categoriaOrigenId;

            // Mostrar el nombre de la categoría a desactivar
            txtMensaje.Text = $"La categoría '{categoriaOrigenNombre}' tiene inventario. Debe reasignar los productos a otra categoría activa para continuar:";

            // Llenar el ComboBox con las categorías activas (excluyendo la original)
            cmbCategorias.ItemsSource = categoriasActivas;
            cmbCategorias.DisplayMemberPath = "name"; // Mostrar el nombre
            cmbCategorias.SelectedValuePath = "category_id"; // Obtener la ID
            cmbCategorias.SelectedIndex = -1; // No seleccionar nada por defecto
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCategorias.SelectedValue == null)
            {
                MessageBox.Show("Por favor, seleccione la categoría de destino.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Guardamos el ID seleccionado y cerramos la ventana con resultado OK
            CategoriaDestinoId = (int)cmbCategorias.SelectedValue;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            // Cerramos la ventana con resultado Cancelar
            DialogResult = false;
        }
    }
}