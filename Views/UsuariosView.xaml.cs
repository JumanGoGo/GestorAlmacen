using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models;
using GestorAlmacen.Helpers;

namespace GestorAlmacen.Views
{
    public partial class UsuariosView : UserControl
    {
        private User _seleccionado;

        public UsuariosView()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            using (var db = new WMS_DBEntities())
            {
                dgUsuarios.ItemsSource = db.Users.OrderBy(u => u.username).ToList();
            }
        }

        private void dgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is User u)
            {
                _seleccionado = u;
                txtUsername.Text = u.username;
                txtUsername.IsEnabled = false;
                txtDisplayName.Text = u.display_name;
                chkActivo.IsChecked = u.is_active;

                // Seleccionar rol en combo
                foreach (ComboBoxItem item in cmbRol.Items)
                {
                    if (item.Content.ToString() == u.role)
                    {
                        cmbRol.SelectedItem = item;
                        break;
                    }
                }

                txtPassword.Password = "";
                lblPasswordHint.Visibility = Visibility.Visible;
                btnGuardar.Content = "Actualizar";
                btnEliminar.Visibility = Visibility.Visible;
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e) => Limpiar();

        private void Limpiar()
        {
            _seleccionado = null;
            txtUsername.Text = ""; txtUsername.IsEnabled = true;
            txtDisplayName.Text = ""; txtPassword.Password = "";
            cmbRol.SelectedIndex = -1;
            chkActivo.IsChecked = true;
            lblPasswordHint.Visibility = Visibility.Collapsed;
            dgUsuarios.SelectedItem = null;
            btnGuardar.Content = "Crear Usuario";
            btnEliminar.Visibility = Visibility.Collapsed;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) return;
            if (cmbRol.SelectedItem == null) return;

            using (var db = new WMS_DBEntities())
            {
                string rol = ((ComboBoxItem)cmbRol.SelectedItem).Content.ToString();

                if (_seleccionado == null)
                {
                    if (string.IsNullOrWhiteSpace(txtPassword.Password))
                    {
                        MessageBox.Show("Contraseña requerida."); return;
                    }

                    // APLICAR HASH AQUÍ
                    string passHash = SecurityHelper.ComputeSha256Hash(txtPassword.Password);

                    var nuevo = new User
                    {
                        username = txtUsername.Text.Trim(),
                        password_hash = passHash, // Guardamos el HASH
                        display_name = txtDisplayName.Text.Trim(),
                        role = rol,
                        is_active = true,
                        created_at = DateTime.Now
                    };
                    db.Users.Add(nuevo);
                }
                else
                {
                    var edit = db.Users.Find(_seleccionado.user_id);
                    edit.display_name = txtDisplayName.Text;
                    edit.role = rol;
                    edit.is_active = (bool)chkActivo.IsChecked;


                    if (!string.IsNullOrEmpty(txtPassword.Password))
                        edit.password_hash = SecurityHelper.ComputeSha256Hash(txtPassword.Password);
                }

                try { db.SaveChanges(); Limpiar(); CargarDatos(); }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null) return;
            if (MessageBox.Show("¿Bloquear acceso a usuario?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new WMS_DBEntities())
                {
                    var u = db.Users.Find(_seleccionado.user_id);
                    u.is_active = false;
                    db.SaveChanges();
                    Limpiar();
                    CargarDatos();
                }
            }
        }
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}