using System.Linq;
using System.Windows;
using System.Windows.Media; // Para los colores
using GestorAlmacen.Models;

namespace GestorAlmacen.Windows
{
    public partial class DetalleMovimientoWindow : Window
    {
        public DetalleMovimientoWindow(int movementId)
        {
            InitializeComponent();
            CargarDatos(movementId);
        }

        private void CargarDatos(int id)
        {
            using (var db = new WMS_DBEntities())
            {
                // 1. Obtener la cabecera del movimiento incluyendo Usuario
                var mov = db.Movements.Include("User").FirstOrDefault(m => m.movement_id == id);

                if (mov == null)
                {
                    MessageBox.Show("No se encontró el movimiento.");
                    this.Close();
                    return;
                }

                // Llenar datos de cabecera
                txtFolio.Text = mov.folio;
                txtFecha.Text = mov.movement_date.ToString("dd/MM/yyyy HH:mm");
                txtUsuario.Text = mov.User != null ? mov.User.username : "Desconocido";
                txtTipo.Text = mov.movement_type;
                txtComentario.Text = mov.comment;
                txtEstado.Text = mov.status;

                // Color del Badge según estado
                if (mov.status == "ACTIVE") badgeEstado.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Verde
                else badgeEstado.Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Rojo

                // 2. Obtener los detalles
                // Usamos una proyección anónima para formatear los datos de la tabla
                var detalles = db.MovementDetails
                                 .Where(d => d.movement_id == id)
                                 .Select(d => new
                                 {
                                     Sku = d.Product.sku,
                                     Producto = d.Product.name,
                                     Area = d.Area.code, // O d.Area.name
                                     Cantidad = d.quantity
                                 })
                                 .ToList();

                dgDetalles.ItemsSource = detalles;
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}