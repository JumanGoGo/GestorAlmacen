using System;
using System.Linq;
using System.Windows;
using GestorAlmacen.Models;
using GestorAlmacen.Helpers;

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
            string passPlano = txtPassword.Password;

            string passHashInput = SecurityHelper.ComputeSha256Hash(passPlano);


            // 1. Validaciones básicas de campos vacíos
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(passPlano))
            {
                MessageBox.Show("Por favor, ingrese usuario y contraseña.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Conexión a Base de Datos
                using (var db = new WMS_DBEntities())
                {
                    // 2. Comparar Hash contra Hash en la base de datos
                    var userEncontrado = db.Users
                        .FirstOrDefault(u => u.username == usuario &&
                                             u.password_hash == passHashInput && // Comparación segura
                                             u.is_active == true);

                    if (userEncontrado != null)
                    {
                        App.UsuarioActual = userEncontrado;

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