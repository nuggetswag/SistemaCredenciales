using Microsoft.Data.Sqlite;
using SistemaCredenciales.Models;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace SistemaCredenciales.Services
{
    public class DatabaseService
    {
        private SQLiteService sqlite =
            new SQLiteService();

        public void LeerMDB(
            string archivoMDB,
            string tablaMDB,
            string area)
        {
            string accessConnectionString =
                $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={archivoMDB};";

            using (OleDbConnection connection =
                new OleDbConnection(accessConnectionString))
            {
                connection.Open();

                string query =
                    $"SELECT * FROM {tablaMDB}";

                OleDbCommand command =
                    new OleDbCommand(query, connection);

                OleDbDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    string matricula =
                        reader["IDWMATRICULA"].ToString();

                    string nombre =
                        reader["IDWNOMBRE"].ToString();

                    string apellidos =
                        reader["IDWAPELLIDOS"].ToString();

                    string vigencia =
                        reader["IDWVIGENCIA"].ToString();

                    using (SqliteConnection sqlConnection =
                        sqlite.ObtenerConexion())
                    {
                        sqlConnection.Open();

                        string verificar =
                            "SELECT COUNT(*) FROM CredencialesImportadas WHERE Matricula = @Matricula";

                        SqliteCommand verificarCommand =
                            new SqliteCommand(
                                verificar,
                                sqlConnection);

                        verificarCommand.Parameters.AddWithValue(
                            "@Matricula",
                            matricula);

                        int existe =
                            Convert.ToInt32(
                                verificarCommand.ExecuteScalar());

                        if (existe == 0)
                        {
                            string insert =
                                @"INSERT INTO CredencialesImportadas
                                (
                                    Matricula,
                                    Nombre,
                                    Apellidos,
                                    Vigencia,
                                    Escuela,
                                    Area
                                )
                                VALUES
                                (
                                    @Matricula,
                                    @Nombre,
                                    @Apellidos,
                                    @Vigencia,
                                    @Escuela,
                                    @Area
                                )";

                            SqliteCommand insertCommand =
                                new SqliteCommand(
                                    insert,
                                    sqlConnection);

                            insertCommand.Parameters.AddWithValue(
                                "@Matricula",
                                matricula);

                            insertCommand.Parameters.AddWithValue(
                                "@Nombre",
                                nombre);

                            insertCommand.Parameters.AddWithValue(
                                "@Apellidos",
                                apellidos);

                            insertCommand.Parameters.AddWithValue(
                                "@Vigencia",
                                vigencia);

                            insertCommand.Parameters.AddWithValue(
                                "@Escuela",
                                "CUDEC");

                            insertCommand.Parameters.AddWithValue(
                                "@Area",
                                area);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<CredencialImportada> ObtenerCredenciales()
        {
            List<CredencialImportada> lista =
                new List<CredencialImportada>();

            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    "SELECT * FROM CredencialesImportadas";

                SqliteCommand command =
                    new SqliteCommand(query, connection);

                SqliteDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new CredencialImportada
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Matricula = reader["Matricula"].ToString(),
                        Nombre = reader["Nombre"].ToString(),
                        Apellidos = reader["Apellidos"].ToString(),
                        Vigencia = reader["Vigencia"].ToString(),
                        Escuela = reader["Escuela"].ToString(),
                        Area = reader["Area"].ToString(),
                        Entregada =
                            Convert.ToBoolean(
                                reader["Entregada"])
                    });
                }
            }

            return lista;
        }

        public void CambiarEstadoEntrega(
            int id,
            bool entregada)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"UPDATE CredencialesImportadas
                    SET Entregada = @Entregada
                    WHERE Id = @Id";

                SqliteCommand command =
                    new SqliteCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Entregada",
                    entregada ? 1 : 0);

                command.Parameters.AddWithValue(
                    "@Id",
                    id);

                command.ExecuteNonQuery();
            }
        }

        public void GuardarRutaFirma(
            int id,
            string rutaFirma)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"UPDATE CredencialesImportadas
                    SET RutaFirma = @RutaFirma
                    WHERE Id = @Id";

                SqliteCommand command =
                    new SqliteCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@RutaFirma",
                    rutaFirma);

                command.Parameters.AddWithValue(
                    "@Id",
                    id);

                command.ExecuteNonQuery();
            }
        }

        public string ObtenerRutaFirma(int id)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"SELECT RutaFirma
                    FROM CredencialesImportadas
                    WHERE Id = @Id";

                SqliteCommand command =
                    new SqliteCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Id",
                    id);

                object resultado =
                    command.ExecuteScalar();

                if (resultado != null)
                {
                    return resultado.ToString();
                }

                return "";
            }
        }

        public void LimpiarRutaFirma(int id)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"UPDATE CredencialesImportadas
                    SET RutaFirma = NULL
                    WHERE Id = @Id";

                SqliteCommand command =
                    new SqliteCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Id",
                    id);

                command.ExecuteNonQuery();
            }
        }
    }
}

