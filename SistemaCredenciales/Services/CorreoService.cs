using System;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Envía códigos de verificación por correo (SMTP, p. ej. Gmail).
    /// </summary>
    public class CorreoService
    {
        public void EnviarCodigo(string destino, string codigo, string motivo)
        {
            AppConfig cfg = AppConfig.Actual;

            string usuario = cfg.CorreoUsuario;
            string pass = cfg.ObtenerAppPassword();

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pass))
                throw new Exception(
                    "Falta configurar el correo y su contraseña de aplicación.");

            using (var mensaje = new MailMessage(usuario, destino))
            {
                mensaje.Subject = "Código de verificación – Sistema de Credenciales";
                mensaje.Body =
                    $"Solicitaste: {motivo}\n\n" +
                    $"Tu código de verificación es:  {codigo}\n\n" +
                    "Vence en 10 minutos. Si no fuiste tú, ignora este correo.";

                using (var smtp = new SmtpClient(cfg.SmtpHost, cfg.SmtpPuerto))
                {
                    smtp.EnableSsl = cfg.SmtpSsl;
                    smtp.Credentials = new NetworkCredential(usuario, pass);
                    smtp.Timeout = 20000;
                    smtp.Send(mensaje);
                }
            }
        }
    }

    /// <summary>
    /// Genera y valida un código de verificación de 6 dígitos (un solo uso,
    /// vence a los 10 minutos). Se guarda solo en memoria.
    /// </summary>
    public static class CodigoVerificacion
    {
        private static string? _codigo;
        private static DateTime _expira;

        public static string Generar()
        {
            _codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _expira = DateTime.Now.AddMinutes(10);
            return _codigo;
        }

        public static bool Validar(string? ingresado)
        {
            if (_codigo == null || DateTime.Now > _expira)
                return false;

            bool ok = string.Equals((ingresado ?? "").Trim(), _codigo);

            if (ok)
                _codigo = null; // un solo uso

            return ok;
        }
    }
}
