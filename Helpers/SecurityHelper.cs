using System.Security.Cryptography;
using System.Text;

namespace GestorAlmacen.Helpers
{
    public static class SecurityHelper
    {
        // Método para convertir texto plano a Hash SHA256
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Computar Hash - devuelve array de bytes
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convertir array de bytes a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}