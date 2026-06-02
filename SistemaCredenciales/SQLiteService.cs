using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace SistemaCredenciales.Services
{
    public class SQLiteService
    {
        private string dbPath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SistemaCredenciales.db");

        private string connectionString;

        public SQLiteService()
        {
            connectionString =
                $"Data Source={dbPath}";

            CrearBaseDatos();
        }

        private void CrearBaseDatos()
        {
            using (SqliteConnection connection =
                new SqliteConnection(connectionString))
            {
                connection.Open();

                string tabla =
                    @"
                    CREATE TABLE IF NOT EXISTS CredencialesImportadas
                    (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Matricula TEXT,
                        Nombre TEXT,
                        Apellidos TEXT,
                        Vigencia TEXT,
                        Escuela TEXT,
                        Area TEXT,
                        Entregada INTEGER DEFAULT 0,
                        RutaFirma TEXT,
                        ArchivoOrigen TEXT
                    );
                    ";

                SqliteCommand command =
                    new SqliteCommand(
                        tabla,
                        connection);

                command.ExecuteNonQuery();
            }
        }

        public SqliteConnection ObtenerConexion()
        {
            return new SqliteConnection(
                connectionString);
        }
    }
}