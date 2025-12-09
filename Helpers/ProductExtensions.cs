using System.Linq;

namespace GestorAlmacen.Models
{

    public partial class Product
    {
        // Esta propiedad no está en la BD, pero WPF la podrá leer
        public int StockCalculado
        {
            get
            {
                // Si la lista de Stocks es nula, retornamos 0
                if (Stocks == null) return 0;

                // Sumamos la cantidad (quantity) de todos los registros de stock
                // Asumiendo que tu tabla Stock tiene un campo 'quantity'
                return Stocks.Sum(s => s.quantity);
            }
        }

        // Opcional: Propiedad auxiliar para mostrar "Activo/Inactivo" en texto
        public string EstadoTexto => is_active ? "Activo" : "Inactivo";
    }
}