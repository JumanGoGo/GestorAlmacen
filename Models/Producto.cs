
public class Producto
{
    public string SKU { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string Observaciones { get; set; }
   
    public string PrecioFormateado => $"${Precio:N2}";
}