using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestorAlmacen.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            // AQUÍ CONECTARÍAS CON TU BASE DE DATOS PARA VALIDAR ROL
            string usuario = txtUsuario.Text;
            string pass = txtPassword.Password;

            // Validación simulada
            if (usuario == "admin" && pass == "123")
            {
                MainWindow main = new MainWindow("Administrador"); // Pasamos el rol
                main.Show();
                this.Close();
            }
            else if (usuario == "user" && pass == "123")
            {
                MainWindow main = new MainWindow("Usuario");
                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Credenciales incorrectas.");
            }
        }
    }


}
