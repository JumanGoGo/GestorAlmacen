using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Views;

namespace GestorAlmacen
{
    public partial class MainWindow : Window
    {
        private string _userRole;

        public MainWindow(string role)
        {
            InitializeComponent();
            _userRole = role;

            // 1. Configuramos qué puede ver el usuario
            ConfigurarPermisos();

            // 2. Cargamos la vista inicial
            MainContent.Navigate(new InventarioView());
        }

        private void ConfigurarPermisos()
        {
            // Reglas basadas en tu Documentación:
            // ADMIN: Acceso Total.
            // SUPERVISOR: Gestión de Productos/Áreas, pero NO Usuarios.
            // OPERADOR: Solo Movimientos e Inventario. NO configuración.

            // Por defecto asumimos que todo es visible, y ocultamos según restricción

            if (_userRole == "OPERADOR")
            {
                // El Operador solo mueve mercancía, no configura el sistema
                btnProductos.Visibility = Visibility.Collapsed;
                btnAreas.Visibility = Visibility.Collapsed;
                btnCategorias.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;
            }
            else if (_userRole == "SUPERVISOR")
            {
                // El Supervisor gestiona el almacén, pero no la seguridad del sistema
                btnUsuarios.Visibility = Visibility.Collapsed;

                // Aseguramos que vea lo demás (por si acaso)
                btnProductos.Visibility = Visibility.Visible;
                btnAreas.Visibility = Visibility.Visible;
                btnCategorias.Visibility = Visibility.Visible;
            }
            else if (_userRole == "ADMIN")
            {
                // El Admin ve todo
                btnUsuarios.Visibility = Visibility.Visible;
                btnProductos.Visibility = Visibility.Visible;
                btnAreas.Visibility = Visibility.Visible;
                btnCategorias.Visibility = Visibility.Visible;
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            string tag = ((Button)sender).Tag.ToString();

            switch (tag)
            {
                // --- VISTAS OPERATIVAS (Todos) ---
                case "Inventario":
                    MainContent.Navigate(new InventarioView());
                    break;

                case "Movimientos": // Historial completo
                    MainContent.Navigate(new MovimientosView("TODOS"));
                    break;

                case "Entradas": // Filtro preestablecido
                    MainContent.Navigate(new MovimientosView("ENTR"));
                    break;

                case "Salidas": // Filtro preestablecido
                    MainContent.Navigate(new MovimientosView("SAL"));
                    break;

                // --- VISTAS DE GESTIÓN (Supervisor/Admin) ---
                case "Productos":
                    MainContent.Navigate(new ProductosView());
                    break;

                case "Areas":
                    MainContent.Navigate(new AreasView());
                    break;

                case "Categorias":
                    MainContent.Navigate(new CategoriasView());
                    break;

                // --- VISTAS ADMINISTRATIVAS (Solo Admin) ---
                case "Usuarios":
                    MainContent.Navigate(new UsuariosView());
                    break;

                default:
                    MessageBox.Show("Esta vista aún no está implementada.");
                    break;
            }
        }

        private void Salir_Click(object sender, RoutedEventArgs e)
        {
            // Cerrar sesión y volver al Login
            LoginView login = new LoginView();
            login.Show();
            this.Close();
        }
    }
}