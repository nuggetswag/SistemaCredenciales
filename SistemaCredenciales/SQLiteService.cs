using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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
                        ArchivoOrigen TEXT,
                        FechaEntrega TEXT
                    );
                    ";

                SqliteCommand command =
                    new SqliteCommand(
                        tabla,
                        connection);

                command.ExecuteNonQuery();

                string bitacora =
                    @"
                    CREATE TABLE IF NOT EXISTS Bitacora
                    (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Fecha TEXT,
                        Usuario TEXT,
                        Accion TEXT,
                        Detalle TEXT
                    );
                    ";

                using (var cmdBitacora = new SqliteCommand(bitacora, connection))
                    cmdBitacora.ExecuteNonQuery();

                Migrar(connection);
            }
        }

        /// <summary>
        /// Aplica cambios de esquema a bases de datos ya existentes:
        /// agrega columnas nuevas que falten y normaliza datos antiguos.
        /// </summary>
        private void Migrar(SqliteConnection connection)
        {
            // Agregar columnas nuevas solo si no existen todavía.
            var columnas = new List<string>();

            using (SqliteCommand info =
                new SqliteCommand(
                    "PRAGMA table_info(CredencialesImportadas);",
                    connection))
            using (SqliteDataReader reader = info.ExecuteReader())
            {
                while (reader.Read())
                    columnas.Add(reader["name"]?.ToString() ?? "");
            }

            AgregarColumnaSiFalta(connection, columnas, "FechaEntrega", "TEXT");
            AgregarColumnaSiFalta(connection, columnas, "ArchivoOrigen", "TEXT");

            // Datos antiguos: la escuela se guardaba fija como "CUDEC" y el nombre
            // real quedaba en Area. Se rellena Escuela con Area para poder filtrar.
            using (SqliteCommand backfill =
                new SqliteCommand(
                    @"UPDATE CredencialesImportadas
                      SET Escuela = Area
                      WHERE (Escuela IS NULL OR Escuela = '' OR Escuela = 'CUDEC')
                        AND Area IS NOT NULL AND Area <> '';",
                    connection))
            {
                backfill.ExecuteNonQuery();
            }
        }

        private void AgregarColumnaSiFalta(
            SqliteConnection connection,
            List<string> columnasExistentes,
            string nombre,
            string tipo)
        {
            if (columnasExistentes.Contains(nombre))
                return;

            using (SqliteCommand command =
                new SqliteCommand(
                    $"ALTER TABLE CredencialesImportadas ADD COLUMN {nombre} {tipo};",
                    connection))
            {
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