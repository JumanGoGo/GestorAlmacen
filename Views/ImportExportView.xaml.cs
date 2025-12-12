using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GestorAlmacen.Models;

namespace GestorAlmacen.Views
{
    public partial class ImportExportView : UserControl
    {
        private string _rutaArchivo;

        public ImportExportView()
        {
            InitializeComponent();
            CargarComboAreas();

            // Evento para cambiar la instrucción de formato según lo seleccionado
            cmbTipoImportacion.SelectionChanged += (s, e) =>
            {
                if (cmbTipoImportacion.SelectedItem is ComboBoxItem item)
                {
                    string tag = item.Tag.ToString();
                    switch (tag)
                    {
                        case "CAT": lblFormato.Text = "Formato: NombreCategoria, Descripcion"; break;
                        case "AREA": lblFormato.Text = "Formato: Codigo, Nombre, Capacidad (Opc), Categoria (Opc)"; break;
                        case "PROD": lblFormato.Text = "Formato: SKU, Nombre, NombreCategoria"; break;
                        case "MOV": lblFormato.Text = "Formato: SKU, CodigoArea, Cantidad"; break;
                    }
                }
            };
        }

        private void CargarComboAreas()
        {
            using (var db = new WMS_DBEntities())
            {
                var areas = db.Areas.Where(a => a.is_active).ToList();
                cmbAreaReporte.Items.Add("Todas");
                foreach (var a in areas) cmbAreaReporte.Items.Add(a.code);
                cmbAreaReporte.SelectedIndex = 0;
            }
        }

        // --- IMPORTACIÓN ---

        private void btnSeleccionar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Archivos CSV (*.csv)|*.csv";
            if (openFileDialog.ShowDialog() == true)
            {
                _rutaArchivo = openFileDialog.FileName;
                txtArchivo.Text = _rutaArchivo;
                btnProcesar.IsEnabled = true;
            }
        }

        private void btnProcesar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTipoImportacion.SelectedItem == null) return;
            string tipo = ((ComboBoxItem)cmbTipoImportacion.SelectedItem).Tag.ToString();

            try
            {
                var lineas = File.ReadAllLines(_rutaArchivo);
                int procesados = 0;
                int errores = 0;
                StringBuilder log = new StringBuilder();

                using (var db = new WMS_DBEntities())
                {
                    // Empezamos en i=1 para saltar encabezados.
                    for (int i = 1; i < lineas.Length; i++)
                    {
                        string linea = lineas[i];
                        if (string.IsNullOrWhiteSpace(linea)) continue;

                        var datos = linea.Split(','); // Separador coma

                        try
                        {
                            switch (tipo)
                            {
                                case "CAT": ImportarCategoria(db, datos); break;
                                case "AREA": ImportarArea(db, datos); break;
                                case "PROD": ImportarProducto(db, datos); break;
                                case "MOV": ImportarMovimiento(db, datos); break;
                            }
                            procesados++;
                        }
                        catch (Exception ex)
                        {
                            errores++;
                            log.AppendLine($"Error línea {i + 1}: {ex.Message}");
                        }
                    }
                    db.SaveChanges(); // Guardamos todo al final
                }

                txtLog.Text = $"Proceso finalizado.\nCorrectos: {procesados}\nErrores: {errores}\n\nDetalle Errores:\n{log.ToString()}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al leer archivo: " + ex.Message);
            }
        }

        // LÓGICA ESPECÍFICA DE MAPEO
        private void ImportarCategoria(WMS_DBEntities db, string[] d)
        {
            string nombre = d[0].Trim();
            if (!db.Categories.Any(c => c.name == nombre))
            {
                db.Categories.Add(new Category { name = nombre, description = d.Length > 1 ? d[1] : "", is_active = true });
            }
        }

        // EN: ImportExportView.xaml.cs

        private void ImportarArea(WMS_DBEntities db, string[] d)
        {
            // Esperamos al menos 2 campos obligatorios: Codigo y Nombre
            if (d.Length < 2)
            {
                throw new Exception("Formato incorrecto. Mínimo requerido: CodigoArea, NombreArea");
            }

            string codigo = d[0].Trim();
            string nombre = d[1].Trim();

            if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(nombre))
            {
                throw new Exception("El código y nombre del área son obligatorios.");
            }

            // --- PROCESAMIENTO DE CAMPOS OPCIONALES ---

            // 1. Capacidad (Columna 3 - Índice 2)
            int? capacidad = null;
            if (d.Length > 2 && int.TryParse(d[2].Trim(), out int cap))
            {
                if (cap > 0) capacidad = cap;
            }

            // 2. Categoría Preferida (Columna 4 - Índice 3)
            int? idCategoria = null;
            if (d.Length > 3 && !string.IsNullOrWhiteSpace(d[3]))
            {
                string nombreCat = d[3].Trim();
                // Buscamos la categoría por nombre en la BD
                var cat = db.Categories.FirstOrDefault(c => c.name == nombreCat);

                // Opcional: Si la categoría no existe, ¿lanzamos error o la ignoramos?
                // Aquí decidimos lanzarlo para que el usuario sepa que hay un error de datos.
                if (cat != null)
                {
                    idCategoria = cat.category_id;
                }
                else
                {
                    throw new Exception($"La categoría preferida '{nombreCat}' no existe. Cárguela primero.");
                }
            }

            // --- CREACIÓN DEL ÁREA ---

            if (!db.Areas.Any(a => a.code == codigo))
            {
                Area nuevaArea = new Area
                {
                    code = codigo,
                    name = nombre,
                    capacity = capacidad,            // Asignamos el valor procesado
                    preferred_category_id = idCategoria, // Asignamos el ID encontrado
                    is_active = true,
                    created_at = DateTime.Now
                };

                db.Areas.Add(nuevaArea);
            }
            else
            {
                throw new Exception($"El Área '{codigo}' ya existe.");
            }
        }
        private void ImportarProducto(WMS_DBEntities db, string[] d)
        {
            string sku = d[0].Trim();
            string nombreProd = d[1].Trim();
            string nombreCat = d[2].Trim();

            // Buscamos ID de categoría
            var cat = db.Categories.FirstOrDefault(c => c.name == nombreCat);
            if (cat == null) throw new Exception($"Categoría '{nombreCat}' no existe.");

            if (!db.Products.Any(p => p.sku == sku))
            {
                db.Products.Add(new Product
                {
                    sku = sku,
                    name = nombreProd,
                    category_id = cat.category_id,
                    is_active = true,
                    created_at = DateTime.Now
                });
            }
        }

        private void ImportarMovimiento(WMS_DBEntities db, string[] d)
        {
         

            string sku = d[0].Trim();
            string codigoArea = d[1].Trim();
            int cantidad = int.Parse(d[2].Trim());

            var prod = db.Products.FirstOrDefault(p => p.sku == sku);
            var area = db.Areas.FirstOrDefault(a => a.code == codigoArea);

            if (prod == null) throw new Exception($"SKU {sku} no existe");
            if (area == null) throw new Exception($"Area {codigoArea} no existe");

            // Buscar Stock existente
            var stock = db.Stocks.FirstOrDefault(s => s.product_id == prod.product_id && s.area_id == area.area_id);
            if (stock == null)
            {
                stock = new Stock { product_id = prod.product_id, area_id = area.area_id, quantity = 0, last_update = DateTime.Now };
                db.Stocks.Add(stock);
            }

            stock.quantity += cantidad; // Sumamos (Carga Inicial)
            stock.last_update = DateTime.Now;
        }

        // --- EXPORTACIÓN (REPORTE) ---

        private void btnExportar_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Archivos CSV (*.csv)|*.csv";
            saveFileDialog.FileName = $"Auditoria_Inventario_{DateTime.Now:yyyyMMdd}.csv";

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var db = new WMS_DBEntities())
                {
                    // Consulta similar a InventarioView pero preparada para auditoría
                    var query = from s in db.Stocks
                                join p in db.Products on s.product_id equals p.product_id
                                join a in db.Areas on s.area_id equals a.area_id
                                join c in db.Categories on p.category_id equals c.category_id
                                where s.quantity > 0
                                select new
                                {
                                    Area = a.code,
                                    SKU = p.sku,
                                    Producto = p.name,
                                    Categoria = c.name,
                                    Sistema = s.quantity,
                                    UltimoMov = s.last_update
                                };

                    // Filtro
                    if (cmbAreaReporte.SelectedIndex > 0)
                    {
                        string areaFiltro = cmbAreaReporte.SelectedItem.ToString();
                        query = query.Where(x => x.Area == areaFiltro);
                    }

                    var datos = query.OrderBy(x => x.Area).ThenBy(x => x.SKU).ToList();

                    // Generar CSV
                    StringBuilder csv = new StringBuilder();
                    csv.AppendLine("Area,SKU,Producto,Categoria,Stock Sistema,Conteo Fisico,Diferencia,Ultimo Movimiento");

                    foreach (var item in datos)
                    {
                        // Dejamos comas vacías para 'Conteo Fisico' y 'Diferencia'
                        csv.AppendLine($"{item.Area},{item.SKU},{item.Producto},{item.Categoria},{item.Sistema},,,{item.UltimoMov}");
                    }

                    File.WriteAllText(saveFileDialog.FileName, csv.ToString());
                    MessageBox.Show("Reporte generado con éxito.");
                }
            }
        }
    }
}