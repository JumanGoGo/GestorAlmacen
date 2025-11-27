using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestorAlmacen.Views
{
    public partial class UsuariosView : UserControl
    {
        // Mock de datos
        public class UsuarioMock
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string DisplayName { get; set; }
            public string Role { get; set; } // ADMIN, SUPERVISOR, OPERADOR
            public bool IsActive { get; set; }
        }

        private List<UsuarioMock> _listaUsuarios;
        private UsuarioMock _seleccionado;

        public UsuariosView()
        {
            InitializeComponent();
            CargarDatosPrueba();
        }

        private void CargarDatosPrueba()
        {
            _listaUsuarios = new List<UsuarioMock>
            {
                new UsuarioMock { Id=1, Username="admin", DisplayName="Administrador Principal", Role="ADMIN", IsActive=true },
                new UsuarioMock { Id=2, Username="jperez", DisplayName="Juan Pérez", Role="SUPERVISOR", IsActive=true },
                new UsuarioMock { Id=3, Username="operador1", DisplayName="Roberto Gómez", Role="OPERADOR", IsActive=true },
                new UsuarioMock { Id=4, Username="invitado", DisplayName="Usuario Temporal", Role="OPERADOR", IsActive=false }
            };
            dgUsuarios.ItemsSource = _listaUsuarios;
        }

        // --- Eventos de UI ---

        private void dgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is UsuarioMock user)
            {
                _seleccionado = user;

                txtUsername.Text = user.Username;
                txtUsername.IsEnabled = false; // No permitimos cambiar el username para evitar conflictos
                txtDisplayName.Text = user.DisplayName;
                chkActivo.IsChecked = user.IsActive;

                // Seleccionar Rol
                foreach (ComboBoxItem item in cmbRol.Items)
                {
                    if (item.Content.ToString() == user.Role)
                    {
                        cmbRol.SelectedItem = item;
                        break;
                    }
                }

                // Manejo visual de contraseña en edición
                txtPassword.Password = "";
                lblPasswordHint.Visibility = Visibility.Visible; // "Dejar vacío para no cambiar"

                btnGuardar.Content = "Actualizar Datos";
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
            txtUsername.Text = "";
            txtUsername.IsEnabled = true;
            txtDisplayName.Text = "";
            txtPassword.Password = "";
            cmbRol.SelectedIndex = -1;
            chkActivo.IsChecked = true;

            lblPasswordHint.Visibility = Visibility.Collapsed; // Ocultar hint en modo nuevo

            dgUsuarios.SelectedItem = null;
            btnGuardar.Content = "Crear Usuario";
            btnEliminar.Visibility = Visibility.Collapsed;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtDisplayName.Text))
            {
                MessageBox.Show("Usuario y Nombre son obligatorios.");
                return;
            }

            if (cmbRol.SelectedItem == null)
            {
                MessageBox.Show("Debe asignar un Rol.");
                return;
            }

            // Validación especial de Password
            if (_seleccionado == null && string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Debe asignar una contraseña para el nuevo usuario.");
                return;
            }

            string rolSeleccionado = ((ComboBoxItem)cmbRol.SelectedItem).Content.ToString();

            if (_seleccionado == null)
            {
                // INSERT
                MessageBox.Show($"Usuario '{txtUsername.Text}' ({rolSeleccionado}) creado con éxito.");
            }
            else
            {
                // UPDATE
                string msgPass = string.IsNullOrEmpty(txtPassword.Password) ? "sin cambiar contraseña" : "y nueva contraseña establecida";
                MessageBox.Show($"Usuario '{txtUsername.Text}' actualizado {msgPass}.");
            }

            LimpiarFormulario();
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado != null)
            {
                var result = MessageBox.Show($"¿Inhabilitar el acceso a '{_seleccionado.Username}'?",
                                             "Confirmar Bloqueo", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Lógica SQL: UPDATE Users SET is_active = 0 WHERE user_id = ...
                    MessageBox.Show("Usuario inhabilitado.");
                    LimpiarFormulario();
                }
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = txtBuscar.Text.ToLower();
            if (_listaUsuarios != null)
            {
                var filtrado = _listaUsuarios.Where(x => x.Username.ToLower().Contains(query) ||
                                                         x.DisplayName.ToLower().Contains(query)).ToList();
                dgUsuarios.ItemsSource = filtrado;
            }
        }
    }
}
