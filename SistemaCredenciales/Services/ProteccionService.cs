using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Cifra/descifra textos sensibles (como la contraseña de aplicación del
    /// correo) usando DPAPI de Windows, atado al usuario actual. Así no se
    /// guardan en texto plano en config.json.
    /// </summary>
    public static class ProteccionService
    {
        // Entropía extra para que el dato solo lo descifre esta app.
        private static readonly byte[] Entropia =
            Encoding.UTF8.GetBytes("SistemaCredenciales-CUDEC");

        public static string Proteger(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano))
                return "";

            try
            {
                byte[] datos = Encoding.UTF8.GetBytes(textoPlano);

                byte[] cifrado = ProtectedData.Protect(
                    datos, Entropia, DataProtectionScope.CurrentUser);

                return Convert.ToBase64String(cifrado);
            }
            catch
            {
                return "";
            }
        }

        public static string Desproteger(string textoCifrado)
        {
            if (string.IsNullOrEmpty(textoCifrado))
                return "";

            try
            {
                byte[] cifrado = Convert.FromBase64String(textoCifrado);

                byte[] datos = ProtectedData.Unprotect(
                    cifrado, Entropia, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(datos);
            }
            catch
            {
                // Si se movió a otro usuario/PC, no se puede descifrar.
                return "";
            }
        }
    }
}
