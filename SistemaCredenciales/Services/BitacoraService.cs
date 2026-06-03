using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace SistemaCredenciales.Services
{
    /// <summary>Una entrada de la bitácora.</summary>
    public class RegistroBitacora
    {
        public string Fecha { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Accion { get; set; } = "";
        public string Detalle { get; set; } = "";
    }

    /// <summary>
    /// Bitácora simple: registra acciones importantes con fecha y el usuario de
    /// Windows. Se guarda en la propia base (entra en los respaldos).
    /// </summary>
    public class BitacoraService
    {
        private readonly SQLiteService sqlite = new SQLiteService();

        public void Registrar(string accion, string detalle = "")
        {
            try
            {
                using (SqliteConnection connection = sqlite.ObtenerConexion())
                {
                    connection.Open();

                    using (var command = new SqliteCommand(
                        @"INSERT INTO Bitacora (Fecha, Usuario, Accion, Detalle)
                          VALUES (@Fecha, @Usuario, @Accion, @Detalle)",
                        connection))
                    {
                        command.Parameters.AddWithValue(
                            "@Fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Parameters.AddWithValue(
                            "@Usuario", Environment.UserName ?? "");
                        command.Parameters.AddWithValue("@Accion", accion ?? "");
                        command.Parameters.AddWithValue("@Detalle", detalle ?? "");

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // La bitácora nunca debe interrumpir la operación principal.
            }
        }

        public List<RegistroBitacora> Obtener(int maximo = 1000)
        {
            var lista = new List<RegistroBitacora>();

            using (SqliteConnection connection = sqlite.ObtenerConexion())
            {
                connection.Open();

                using (var command = new SqliteCommand(
                    @"SELECT Fecha, Usuario, Accion, Detalle FROM Bitacora
                      ORDER BY Id DESC LIMIT @Max", connection))
                {
                    command.Parameters.AddWithValue("@Max", maximo);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new RegistroBitacora
                            {
                                Fecha = reader["Fecha"]?.ToString() ?? "",
                                Usuario = reader["Usuario"]?.ToString() ?? "",
                                Accion = reader["Accion"]?.ToString() ?? "",
                                Detalle = reader["Detalle"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return lista;
        }
    }
}
