using System.Windows;
using System.Windows.Controls;
using GestorAlmacen.Models; // Necesario para usar la clase 'User'
using GestorAlmacen.Views;

namespace GestorAlmacen
{
    public partial class MainWindow : Window
    {
        private User _usuarioActual; // Guardamos el usuario completo
        private string _userRole;    // Mantenemos esta variable para tu lógica de permisos

        // Propiedades públicas para el XAML (Binding)
        public string NombreUsuario { get; set; }
        public string RolUsuario { get; set; }

        // Constructor modificado: recibe 'User' en vez de 'string'
        public MainWindow(User usuario)
        {
            InitializeComponent();

            _usuarioActual = usuario;
            _userRole = usuario.role; // Asignamos el rol para la lógica interna

            // Preparamos datos para la vista
            NombreUsuario = usuario.display_name;
            RolUsuario = usuario.role;

            // Permite que el XAML lea las propiedades de esta clase
            this.DataContext = this;

            // Configuración inicial
            ConfigurarPermisos();
            MainContent.Navigate(new InventarioView());
        }

        private void ConfigurarPermisos()
        {
            // LÓGICA DE PERMISOS

            // 1. Caso OPERADOR: Acceso muy restringido
            if (_userRole == "OPERADOR")
            {
                // Ocultar Configuración
                lblConfigHeader.Visibility = Visibility.Collapsed;
                btnProductos.Visibility = Visibility.Collapsed;
                btnAreas.Visibility = Visibility.Collapsed;
                btnCategorias.Visibility = Visibility.Collapsed;

                // Ocultar Admin Usuarios
                lblAdminHeader.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;

                // Ocultar Herramientas (Importar/Exportar)
                btnImportExport.Visibility = Visibility.Collapsed;
            }
            // 2. Caso SUPERVISOR: Operativo + Config + Herramientas (Sin Usuarios)
            else if (_userRole == "SUPERVISOR")
            {
                // Ocultar Admin Usuarios
                lblAdminHeader.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;

                // Mostrar Configuración
                lblConfigHeader.Visibility = Visibility.Visible;
                btnProductos.Visibility = Visibility.Visible;
                btnAreas.Visibility = Visibility.Visible;
                btnCategorias.Visibility = Visibility.Visible;

                // Mostrar Herramientas -> REQUERIMIENTO NUEVO
                lblHerramientasHeader.Visibility = Visibility.Visible;
                btnImportExport.Visibility = Visibility.Visible;
            }
            // 3. Caso ADMIN: Acceso Total
            else if (_userRole == "ADMIN")
            {
                // Mostrar Todo explícitamente
                lblConfigHeader.Visibility = Visibility.Visible;
                btnProductos.Visibility = Visibility.Visible;
                btnAreas.Visibility = Visibility.Visible;
                btnCategorias.Visibility = Visibility.Visible;

                lblAdminHeader.Visibility = Visibility.Visible;
                btnUsuarios.Visibility = Visibility.Visible;

                lblHerramientasHeader.Visibility = Visibility.Visible;
                btnImportExport.Visibility = Visibility.Visible;
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            string tag = ((Button)sender).Tag.ToString();

            switch (tag)
            {
                case "Inventario": MainContent.Navigate(new InventarioView()); break;
                case "Movimientos": MainContent.Navigate(new MovimientosView("TODOS")); break;
                case "Entradas": MainContent.Navigate(new MovimientosView("ENTR")); break;
                case "Salidas": MainContent.Navigate(new MovimientosView("SAL")); break;

                case "Productos": MainContent.Navigate(new ProductosView()); break;
                case "Areas": MainContent.Navigate(new AreasView()); break;
                case "Categorias": MainContent.Navigate(new CategoriasView()); break;
                case "Usuarios": MainContent.Navigate(new UsuariosView()); break;

                case "ImportExport": MainContent.Navigate(new ImportExportView()); break;

                default: MessageBox.Show("Vista no implementada."); break;
            }
        }

        private void Salir_Click(object sender, RoutedEventArgs e)
        {
            LoginView login = new LoginView();
            login.Show();
            this.Close();
        }
    }
}