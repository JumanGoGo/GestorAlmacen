using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestorAlmacen.Helpers
{
    // Convierte True -> "Activo", False -> "Inactivo"
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
                return activo ? "Activo" : "Inhabilitado";
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convierte True -> Color Verde, False -> Color Rojo
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
                return activo ? Brushes.Green : Brushes.Red;
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}