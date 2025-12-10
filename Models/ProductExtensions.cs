using System.Linq;
using System.ComponentModel.DataAnnotations.Schema; // Necesario para [NotMapped]


namespace GestorAlmacen.Models
{
    public partial class Product
    {
        // El atributo [NotMapped] es buena práctica para asegurar que EF 
        // no intente buscar esto en la base de datos SQL.
        [NotMapped]
        public int StockCalculado
        {
            get
            {
                // Validación de nulos para evitar errores si Stocks no se cargó
                if (this.Stocks == null) return 0;

                return this.Stocks.Sum(s => s.quantity);
            }
        }
    }
}