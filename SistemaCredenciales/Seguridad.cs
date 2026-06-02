using SistemaCredenciales.Services;
using System;
using System.Windows;

namespace SistemaCredenciales
{
    /// <summary>
    /// Flujos de seguridad compartidos (restablecer/cambiar contraseña por
    /// código enviado al correo).
    /// </summary>
    public static class Seguridad
    {
        /// <summary>
        /// Envía un código al correo configurado y, si es correcto, permite
        /// fijar una contraseña nueva. Devuelve true si se cambió.
        /// </summary>
        public static bool RestablecerContrasena(Window owner)
        {
            AppConfig cfg = AppConfig.Actual;

            if (!cfg.CorreoConfigurado())
            {
                Dialogo.Show(
                    "No hay un correo configurado para restablecer la contraseña.\n\n" +
                    "Entra a Configuración con la contraseña actual y configura el " +
                    "correo de verificación primero.",
                    "Sin correo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 1) Enviar código al correo configurado.
            try
            {
                string codigo = CodigoVerificacion.Generar();
                new CorreoService().EnviarCodigo(
                    cfg.CorreoUsuario, codigo, "restablecer la contraseña");
            }
            catch (Exception ex)
            {
                Dialogo.Show("No se pudo enviar el código:\n\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // 2) Pedir el código.
            var ventanaCodigo = new ClaveWindow(
                "Te enviamos un código a tu correo. Escríbelo para restablecer la contraseña.")
            {
                Owner = owner
            };

            if (ventanaCodigo.ShowDialog() != true)
                return false;

            if (!CodigoVerificacion.Validar(ventanaCodigo.Clave))
            {
                Dialogo.Show("Código incorrecto o vencido.", "Acceso denegado",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // 3) Nueva contraseña (dos veces).
            var p1 = new ClaveWindow("Escribe la NUEVA contraseña.") { Owner = owner };
            if (p1.ShowDialog() != true)
                return false;

            if (string.IsNullOrWhiteSpace(p1.Clave))
            {
                Dialogo.Show("La contraseña no puede quedar vacía.");
                return false;
            }

            var p2 = new ClaveWindow("Repite la NUEVA contraseña.") { Owner = owner };
            if (p2.ShowDialog() != true)
                return false;

            if (p1.Clave != p2.Clave)
            {
                Dialogo.Show("Las contraseñas no coinciden.");
                return false;
            }

            cfg.ClaveMaestra = p1.Clave;
            cfg.Guardar();

            Dialogo.Show("Contraseña restablecida. ✓", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }
    }
}
