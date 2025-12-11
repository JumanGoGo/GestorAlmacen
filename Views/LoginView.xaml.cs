using System;
using System.Linq;
using System.Windows;
using GestorAlmacen.Models; // Importante para acceder a la BD

namespace GestorAlmacen.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string pass = txtPassword.Password;

            // 1. Validaciones básicas de campos vacíos
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Por favor, ingrese usuario y contraseña.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Conexión a Base de Datos
                using (var db = new WMS_DBEntities())
                {
                    // Buscamos coincidencia de Usuario + Password + Que esté Activo
                    var userEncontrado = db.Users
                        .FirstOrDefault(u => u.username == usuario &&
                                             u.password_hash == pass &&
                                             u.is_active == true);

                    if (userEncontrado != null)
                    {
                    

                        MainWindow main = new MainWindow(userEncontrado);
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        // 4. Fallo: Usuario no existe, password mal o usuario inactivo
                        MessageBox.Show("Credenciales incorrectas o usuario inhabilitado.", "Error de Acceso", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores de conexión 
                MessageBox.Show($"Error al conectar con la base de datos:\n{ex.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}