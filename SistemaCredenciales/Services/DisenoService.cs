using Microsoft.Data.Sqlite;
using SistemaCredenciales.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Guarda y carga los diseños de credencial (por tipo + lado) en SQLite.
    /// El diseño se configura una sola vez y sirve para todas las escuelas.
    /// </summary>
    public class DisenoService
    {
        private readonly SQLiteService sqlite = new SQLiteService();

        public static readonly string[] Tipos = { "Alumno", "Docente", "Admin" };
        public static readonly string[] Lados = { "frente", "reverso" };

        public void Guardar(DisenoCredencial diseno)
        {
            using (SqliteConnection connection = sqlite.ObtenerConexion())
            {
                connection.Open();

                string json = JsonSerializer.Serialize(diseno.Campos);

                using (var command = new SqliteCommand(
                    @"INSERT INTO DisenosCredencial (Tipo, Lado, PlantillaRuta, CamposJson)
                      VALUES (@Tipo, @Lado, @Plantilla, @Campos)
                      ON CONFLICT(Tipo, Lado) DO UPDATE SET
                          PlantillaRuta = excluded.PlantillaRuta,
                          CamposJson = excluded.CamposJson",
                    connection))
                {
                    command.Parameters.AddWithValue("@Tipo", diseno.Tipo);
                    command.Parameters.AddWithValue("@Lado", diseno.Lado);
                    command.Parameters.AddWithValue("@Plantilla", diseno.PlantillaRuta ?? "");
                    command.Parameters.AddWithValue("@Campos", json);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Carga el diseño guardado; si no existe, devuelve uno vacío.</summary>
        public DisenoCredencial Obtener(string tipo, string lado)
        {
            using (SqliteConnection connection = sqlite.ObtenerConexion())
            {
                connection.Open();

                using (var command = new SqliteCommand(
                    @"SELECT PlantillaRuta, CamposJson FROM DisenosCredencial
                      WHERE Tipo = @Tipo AND Lado = @Lado", connection))
                {
                    command.Parameters.AddWithValue("@Tipo", tipo);
                    command.Parameters.AddWithValue("@Lado", lado);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var campos = new List<CampoCredencial>();
                            string json = reader["CamposJson"]?.ToString() ?? "";

                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                try
                                {
                                    campos = JsonSerializer
                                        .Deserialize<List<CampoCredencial>>(json)
                                        ?? new List<CampoCredencial>();
                                }
                                catch { }
                            }

                            return new DisenoCredencial
                            {
                                Tipo = tipo,
                                Lado = lado,
                                PlantillaRuta = reader["PlantillaRuta"]?.ToString() ?? "",
                                Campos = campos
                            };
                        }
                    }
                }
            }

            return new DisenoCredencial { Tipo = tipo, Lado = lado };
        }

        /// <summary>Tipos (categorías) que ya tienen algún diseño guardado.</summary>
        public List<string> TiposConDiseno()
        {
            var lista = new List<string>();
            using (SqliteConnection connection = sqlite.ObtenerConexion())
            {
                connection.Open();
                using (var command = new SqliteCommand(
                    "SELECT DISTINCT Tipo FROM DisenosCredencial WHERE Tipo <> '' ORDER BY Tipo",
                    connection))
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        lista.Add(reader["Tipo"]?.ToString() ?? "");
                }
            }
            return lista;
        }

        /// <summary>True si hay un diseño guardado (con plantilla o campos).</summary>
        public bool TieneDiseno(string tipo, string lado)
        {
            DisenoCredencial d = Obtener(tipo, lado);
            return !string.IsNullOrWhiteSpace(d.PlantillaRuta) || d.Campos.Count > 0;
        }
    }
}
