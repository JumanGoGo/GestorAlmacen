using System;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Necesario para validar SKU
using System.Windows;

namespace GestorAlmacen.Views.Windows // Asegúrate que coincida con tu carpeta
{
    public partial class ProductoFormWindow : Window
    {
        // Mock para llenar el combo (luego vendrá de BD)
        public class CategoriaMock
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public ProductoFormWindow()
        {
            InitializeComponent();
            CargarCategorias();
        }

        private void CargarCategorias()
        {
            // Simulamos datos de la tabla Categories
            List<CategoriaMock> categorias = new List<CategoriaMock>
            {
                new CategoriaMock { Id = 1, Nombre = "Celulares" },
                new CategoriaMock { Id = 2, Nombre = "Televisiones" },
                new CategoriaMock { Id = 3, Nombre = "Línea Blanca" },
                new CategoriaMock { Id = 4, Nombre = "Audio/Bocinas" },
                new CategoriaMock { Id = 5, Nombre = "Cómputo" }
            };

            cmbCategoria.ItemsSource = categorias;
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // --- VALIDACIONES (Reglas de Negocio) ---

            // 1. Validar SKU Formato (AAA-000)
            string sku = txtSku.Text.Trim();
            string skuPattern = @"^[A-Z]{3}-\d{3}$"; // 3 Letras, guion, 3 numeros

            if (!Regex.IsMatch(sku, skuPattern))
            {
                MessageBox.Show("El SKU debe tener el formato AAA-000 (Ej: CEL-123).\nTres letras mayúsculas, un guion y tres dígitos.",
                                "Error de Formato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validar Nombre
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Validar Categoría
            if (cmbCategoria.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar una categoría.", "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 4. Validar Reorden (Debe ser numérico)
            if (!string.IsNullOrEmpty(txtReorden.Text) && !int.TryParse(txtReorden.Text, out _))
            {
                MessageBox.Show("El punto de reorden debe ser un número entero.", "Error de Formato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // SI TODO ESTA BIEN:
            MessageBox.Show("¡Producto validado correctamente! \n(Aquí se guardaría en SQL)", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true; // Cierra la ventana devolviendo 'True'
            this.Close();
        }
    }
}