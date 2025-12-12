using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic; // Necesario para List<>
using GestorAlmacen.Models;

namespace GestorAlmacen.Views
{
    public partial class CategoriasView : UserControl
    {
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
                    // Mostramos TODAS, ordenadas por Activas primero
                    var lista = db.Categories
                                  .OrderByDescending(c => c.is_active)
                                  .ThenBy(c => c.name)
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

        // --- BOTÓN GUARDAR (Insertar o Editar) ---
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            using (var db = new WMS_DBEntities())
            {
                // CASO ESPECIAL: Usuario intenta desactivar mediante el CheckBox al editar
                if (_seleccionado != null && chkActivo.IsChecked == false)
                {
                    // Llamamos a la lógica de validación y transferencia
                    // Si retorna false, es que el usuario canceló la operación o no pudo transferir
                    if (!GestionarDesactivacionConTransferencia(db, _seleccionado))
                    {
                        chkActivo.IsChecked = true; // Revertimos visualmente
                        return; // Cancelamos el guardado
                    }
                }

                try
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
                        // UPDATE
                        var catEditar = db.Categories.Find(_seleccionado.category_id);
                        if (catEditar != null)
                        {
                            catEditar.name = txtNombre.Text.Trim();
                            catEditar.description = txtDescripcion.Text.Trim();
                            // El estado se actualiza aquí, ya sea porque pasó la validación o porque sigue activo
                            catEditar.is_active = (bool)chkActivo.IsChecked;
                        }
                    }

                    db.SaveChanges();
                    MessageBox.Show("Guardado correctamente.");
                    LimpiarFormulario();
                    CargarDatos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar: " + ex.Message);
                }
            }
        }

        // --- BOTÓN ELIMINAR (Desactivar) ---
        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null) return;

            // Confirmación inicial simple
            if (MessageBox.Show("¿Desea desactivar esta categoría?", "Confirmar", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using (var db = new WMS_DBEntities())
            {
                // Llamamos a la lógica centralizada
                // Si hay stock, pedirá transferir. Si no, solo devuelve true para continuar.
                if (!GestionarDesactivacionConTransferencia(db, _seleccionado))
                {
                    return; // Usuario canceló en la ventana de transferencia
                }

                // Procedemos a desactivar
                var cat = db.Categories.Find(_seleccionado.category_id);
                if (cat != null)
                {
                    cat.is_active = false;
                    db.SaveChanges();
                    LimpiarFormulario();
                    CargarDatos();
                    MessageBox.Show("Categoría desactivada correctamente.");
                }
            }
        }

        // --- TRANSFERENCIA ---

        /// <summary>
        /// Verifica inventario y gestiona la ventana de transferencia si es necesario.
        /// Retorna TRUE si se puede proceder a desactivar.
        /// Retorna FALSE si el usuario cancela o no se puede realizar la acción.
        /// </summary>
        private bool GestionarDesactivacionConTransferencia(WMS_DBEntities db, Category categoria)
        {
            // 1. Verificamos si hay stock
            bool tieneStock = db.Stocks.Any(s => s.quantity > 0 && s.Product.category_id == categoria.category_id);

            if (!tieneStock) return true; // No hay stock, luz verde para desactivar

            // 2. Si HAY stock, preparamos la lista de destinos posibles
            var categoriasDestino = db.Categories
                .Where(c => c.is_active == true && c.category_id != categoria.category_id)
                .ToList();

            if (!categoriasDestino.Any())
            {
                MessageBox.Show("Hay productos con stock en esta categoría, pero no existen otras categorías activas para transferirlos.\n\nCree una nueva categoría antes de desactivar esta.",
                                "Acción Requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 3. Abrimos la ventana de diálogo
            var ventanaTransferencia = new TransferirCategoriaWindow(categoria.category_id, categoria.name, categoriasDestino);

            if (ventanaTransferencia.ShowDialog() == true)
            {
                // El usuario seleccionó una categoría y dio Aceptar
                int idDestino = ventanaTransferencia.CategoriaDestinoId.Value;

                // Ejecutamos la transferencia de productos
                ReasignarProductos(db, categoria.category_id, idDestino);
                return true; // Luz verde, los productos ya se movieron (en memoria), listos para SaveChanges
            }

            return false; // El usuario canceló la ventana
        }

        private void ReasignarProductos(WMS_DBEntities db, int origenId, int destinoId)
        {
            var productos = db.Products.Where(p => p.category_id == origenId).ToList();
            foreach (var p in productos)
            {
                p.category_id = destinoId;
            }
            // Nota: No hacemos SaveChanges aquí, se hace en el método llamador (btnGuardar o btnEliminar)
            // para que la transferencia y la desactivación sean atómicas (o se guardan las dos o ninguna).
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            using (var db = new WMS_DBEntities())
            {
                var query = txtBuscar.Text.ToLower();
                dgCategorias.ItemsSource = db.Categories
                                             .Where(c => c.name.Contains(query))
                                             .OrderByDescending(c => c.is_active)
                                             .ThenBy(c => c.name)
                                             .ToList();
            }
        }
    }
}