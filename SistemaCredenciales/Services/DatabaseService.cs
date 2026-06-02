using Microsoft.Data.Sqlite;
using SistemaCredenciales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SistemaCredenciales.Services
{
    public class DatabaseService
    {
        private SQLiteService sqlite =
            new SQLiteService();

        public const string FormatoFecha = "yyyy-MM-dd HH:mm:ss";

        // ----------------------------------------------------------------
        //  IMPORTACIÓN DESDE ACCESS (.mdb)
        // ----------------------------------------------------------------

        /// <summary>
        /// Lee una base de Access y agrega sus credenciales a la escuela indicada.
        /// Devuelve cuántas credenciales nuevas se insertaron.
        /// </summary>
        public int LeerMDB(
            string archivoMDB,
            string tablaMDB,
            string escuela)
        {
            string accessConnectionString =
                $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={archivoMDB};";

            string archivoOrigen =
                System.IO.Path.GetFileName(archivoMDB);

            int insertadas = 0;

            using (OleDbConnection connection =
                new OleDbConnection(accessConnectionString))
            {
                connection.Open();

                string query =
                    $"SELECT * FROM [{tablaMDB}]";

                OleDbCommand command =
                    new OleDbCommand(query, connection);

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    // Detecta las columnas por su nombre (sin importar acentos ni
                    // mayúsculas): matrícula / No. de empleado, nombre, apellidos,
                    // vigencia. Así no importa cómo se llamen las columnas del .mdb.
                    var mapa = DetectarColumnas(reader);

                    if (mapa.Matricula < 0)
                    {
                        throw new Exception(
                            "No se encontró una columna de matrícula o número de empleado.\n" +
                            "Columnas encontradas: " +
                            string.Join(", ", NombresColumnas(reader)));
                    }

                    using (SqliteConnection sqlConnection =
                        sqlite.ObtenerConexion())
                    {
                        sqlConnection.Open();

                        while (reader.Read())
                        {
                            string matricula = ValorPorIndice(reader, mapa.Matricula);

                            if (string.IsNullOrWhiteSpace(matricula))
                                continue;

                            var credencial = new CredencialImportada
                            {
                                Matricula = matricula,
                                Nombre = ValorPorIndice(reader, mapa.Nombre),
                                Apellidos = ValorPorIndice(reader, mapa.Apellidos),
                                Vigencia = ValorPorIndice(reader, mapa.Vigencia),
                                Escuela = escuela,
                                Area = escuela
                            };

                            if (InsertarSiNoExiste(
                                    sqlConnection, credencial, archivoOrigen))
                            {
                                insertadas++;
                            }
                        }
                    }
                }
            }

            return insertadas;
        }

        /// <summary>
        /// Devuelve el nombre de la primera tabla "real" de una base de Access.
        /// </summary>
        public string ObtenerTablaMDB(string rutaMDB)
        {
            string connectionString =
                $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={rutaMDB};";

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                DataTable tablas =
                    connection.GetSchema("Tables");

                foreach (DataRow row in tablas.Rows)
                {
                    string nombreTabla =
                        row["TABLE_NAME"]?.ToString() ?? "";

                    if (nombreTabla != "" && !nombreTabla.StartsWith("MSys"))
                        return nombreTabla;
                }
            }

            return "";
        }

        // ----------------------------------------------------------------
        //  IMPORTACIÓN DESDE EXCEL (.xlsx / .xls)
        // ----------------------------------------------------------------

        /// <summary>
        /// Importa credenciales desde un Excel detectando automáticamente las
        /// columnas (Matrícula, Nombre, Apellidos, Vigencia) por su encabezado,
        /// sin importar acentos ni mayúsculas. Devuelve cuántas se insertaron.
        /// </summary>
        public int ImportarExcel(string rutaExcel, string escuela)
        {
            string extension =
                System.IO.Path.GetExtension(rutaExcel).ToLower();

            string propiedades =
                extension == ".xls"
                    ? "Excel 8.0;HDR=YES;IMEX=1"
                    : "Excel 12.0 Xml;HDR=YES;IMEX=1";

            string connectionString =
                $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={rutaExcel};" +
                $"Extended Properties=\"{propiedades}\";";

            string archivoOrigen =
                System.IO.Path.GetFileName(rutaExcel);

            int insertadas = 0;

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                string hoja = ObtenerPrimeraHoja(connection);

                OleDbCommand command =
                    new OleDbCommand($"SELECT * FROM [{hoja}]", connection);

                OleDbDataReader reader =
                    command.ExecuteReader();

                // Detectar a qué columna del Excel corresponde cada campo.
                var mapa = DetectarColumnas(reader);

                if (mapa.Matricula < 0)
                {
                    throw new Exception(
                        "No se encontró una columna de matrícula en el Excel. " +
                        "Columnas encontradas: " +
                        string.Join(", ", NombresColumnas(reader)));
                }

                using (SqliteConnection sqlConnection =
                    sqlite.ObtenerConexion())
                {
                    sqlConnection.Open();

                    while (reader.Read())
                    {
                        string matricula =
                            ValorPorIndice(reader, mapa.Matricula);

                        if (string.IsNullOrWhiteSpace(matricula))
                            continue;

                        var credencial = new CredencialImportada
                        {
                            Matricula = matricula,
                            Nombre = ValorPorIndice(reader, mapa.Nombre),
                            Apellidos = ValorPorIndice(reader, mapa.Apellidos),
                            Vigencia = ValorPorIndice(reader, mapa.Vigencia),
                            Escuela = escuela,
                            Area = escuela
                        };

                        if (InsertarSiNoExiste(
                                sqlConnection,
                                credencial,
                                archivoOrigen))
                        {
                            insertadas++;
                        }
                    }
                }
            }

            return insertadas;
        }

        /// <summary>
        /// Texto que describe qué columnas detectó el Excel, para mostrarlo
        /// como vista previa antes de importar.
        /// </summary>
        public string DescribirColumnasExcel(string rutaExcel)
        {
            string extension =
                System.IO.Path.GetExtension(rutaExcel).ToLower();

            string propiedades =
                extension == ".xls"
                    ? "Excel 8.0;HDR=YES;IMEX=1"
                    : "Excel 12.0 Xml;HDR=YES;IMEX=1";

            string connectionString =
                $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={rutaExcel};" +
                $"Extended Properties=\"{propiedades}\";";

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                string hoja = ObtenerPrimeraHoja(connection);

                OleDbCommand command =
                    new OleDbCommand($"SELECT * FROM [{hoja}]", connection);

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    var nombres = NombresColumnas(reader);
                    var mapa = DetectarColumnas(reader);

                    string Describir(int indice) =>
                        indice >= 0 && indice < nombres.Count
                            ? nombres[indice]
                            : "(no encontrada)";

                    return
                        $"Hoja: {hoja}\n" +
                        $"Matrícula  → {Describir(mapa.Matricula)}\n" +
                        $"Nombre     → {Describir(mapa.Nombre)}\n" +
                        $"Apellidos  → {Describir(mapa.Apellidos)}\n" +
                        $"Vigencia   → {Describir(mapa.Vigencia)}";
                }
            }
        }

        /// <summary>
        /// Describe las columnas reales de una tabla de Access y a qué campo
        /// mapea cada una (para diagnóstico antes de importar).
        /// </summary>
        public string DescribirColumnasMDB(string rutaMDB, string tablaMDB)
        {
            string cs =
                $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={rutaMDB};";

            using (var connection = new OleDbConnection(cs))
            {
                connection.Open();

                using (var command =
                    new OleDbCommand($"SELECT * FROM [{tablaMDB}]", connection))
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    var nombres = NombresColumnas(reader);
                    var mapa = DetectarColumnas(reader);

                    string Describir(int indice) =>
                        indice >= 0 && indice < nombres.Count
                            ? nombres[indice]
                            : "(no encontrada)";

                    return
                        $"Tabla: {tablaMDB}\n" +
                        $"Columnas: {string.Join(", ", nombres)}\n\n" +
                        $"Matrícula  → {Describir(mapa.Matricula)}\n" +
                        $"Nombre     → {Describir(mapa.Nombre)}\n" +
                        $"Apellidos  → {Describir(mapa.Apellidos)}\n" +
                        $"Vigencia   → {Describir(mapa.Vigencia)}";
                }
            }
        }

        private string ObtenerPrimeraHoja(OleDbConnection connection)
        {
            DataTable hojas =
                connection.GetSchema("Tables");

            foreach (DataRow row in hojas.Rows)
            {
                string nombre = row["TABLE_NAME"]?.ToString() ?? "";

                // Las hojas reales terminan en "$"; se ignoran los rangos con nombre.
                if (nombre.EndsWith("$") || nombre.EndsWith("$'"))
                    return nombre.Trim('\'');
            }

            if (hojas.Rows.Count > 0)
                return hojas.Rows[0]["TABLE_NAME"]?.ToString() ?? "";

            throw new Exception("El archivo de Excel no tiene hojas legibles.");
        }

        private struct MapaColumnas
        {
            public int Matricula;
            public int Nombre;
            public int Apellidos;
            public int Vigencia;
        }

        private MapaColumnas DetectarColumnas(OleDbDataReader reader)
        {
            var nombres = NombresColumnas(reader);

            return new MapaColumnas
            {
                Matricula = BuscarColumna(nombres,
                    "matricula", "idwmatricula", "noempleado", "idwnoempleado",
                    "numeroempleado", "numempleado", "numerodeempleado",
                    "matriculaalumno"),
                Nombre = BuscarColumna(nombres,
                    "nombre", "idwnombre", "nombres", "nombrealumno",
                    "nombreempleado"),
                Apellidos = BuscarColumna(nombres,
                    "apellidos", "idwapellidos", "apellido", "apellidopaterno"),
                Vigencia = BuscarColumna(nombres,
                    "vigencia", "idwvigencia", "vence", "fechavigencia",
                    "validohasta")
            };
        }

        private List<string> NombresColumnas(OleDbDataReader reader)
        {
            var nombres = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
                nombres.Add(reader.GetName(i));

            return nombres;
        }

        private int BuscarColumna(List<string> nombres, params string[] alias)
        {
            var normalizados = nombres.Select(Normalizar).ToList();

            // 1) Coincidencia EXACTA (evita confundir "IDWNOMBRE" con matrícula).
            for (int i = 0; i < normalizados.Count; i++)
            {
                foreach (string a in alias)
                {
                    if (normalizados[i] == a)
                        return i;
                }
            }

            // 2) Coincidencia por "contiene" (para prefijos como IDW, sufijos, etc.).
            for (int i = 0; i < normalizados.Count; i++)
            {
                foreach (string a in alias)
                {
                    if (a.Length >= 4 && normalizados[i].Contains(a))
                        return i;
                }
            }

            return -1;
        }

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return "";

            string sinAcentos = texto.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (char c in sinAcentos)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(c);

                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        private string ValorPorIndice(OleDbDataReader reader, int indice)
        {
            if (indice < 0 || indice >= reader.FieldCount)
                return "";

            object valor = reader.GetValue(indice);

            return valor == null || valor == DBNull.Value
                ? ""
                : valor.ToString()?.Trim() ?? "";
        }

        private bool InsertarSiNoExiste(
            SqliteConnection sqlConnection,
            CredencialImportada credencial,
            string archivoOrigen)
        {
            string verificar =
                @"SELECT COUNT(*) FROM CredencialesImportadas
                  WHERE Matricula = @Matricula AND Escuela = @Escuela";

            using (SqliteCommand verificarCommand =
                new SqliteCommand(verificar, sqlConnection))
            {
                verificarCommand.Parameters.AddWithValue(
                    "@Matricula", credencial.Matricula);

                verificarCommand.Parameters.AddWithValue(
                    "@Escuela", credencial.Escuela);

                int existe =
                    Convert.ToInt32(verificarCommand.ExecuteScalar());

                if (existe > 0)
                    return false;
            }

            string insert =
                @"INSERT INTO CredencialesImportadas
                    (Matricula, Nombre, Apellidos, Vigencia, Escuela, Area, ArchivoOrigen)
                  VALUES
                    (@Matricula, @Nombre, @Apellidos, @Vigencia, @Escuela, @Area, @ArchivoOrigen)";

            using (SqliteCommand insertCommand =
                new SqliteCommand(insert, sqlConnection))
            {
                insertCommand.Parameters.AddWithValue("@Matricula", credencial.Matricula);
                insertCommand.Parameters.AddWithValue("@Nombre", credencial.Nombre ?? "");
                insertCommand.Parameters.AddWithValue("@Apellidos", credencial.Apellidos ?? "");
                insertCommand.Parameters.AddWithValue("@Vigencia", credencial.Vigencia ?? "");
                insertCommand.Parameters.AddWithValue("@Escuela", credencial.Escuela ?? "");
                insertCommand.Parameters.AddWithValue("@Area", credencial.Area ?? "");
                insertCommand.Parameters.AddWithValue("@ArchivoOrigen", archivoOrigen ?? "");

                insertCommand.ExecuteNonQuery();
            }

            return true;
        }

        // ----------------------------------------------------------------
        //  CONSULTAS
        // ----------------------------------------------------------------

        /// <summary>
        /// Obtiene las credenciales. Si se indica una escuela, solo devuelve las
        /// de esa escuela; si es null o vacío, devuelve todas.
        /// </summary>
        public List<CredencialImportada> ObtenerCredenciales(string? escuela = null)
        {
            return ObtenerCredencialesFiltradas(escuela, null, null, null);
        }

        /// <summary>
        /// Consulta flexible usada por la lista principal, Entregas y Reportes.
        /// </summary>
        public List<CredencialImportada> ObtenerCredencialesFiltradas(
            string? escuela,
            bool? entregada,
            DateTime? desde,
            DateTime? hasta)
        {
            var lista = new List<CredencialImportada>();

            var condiciones = new List<string>();

            if (!string.IsNullOrEmpty(escuela))
                condiciones.Add("Escuela = @Escuela");

            if (entregada.HasValue)
                condiciones.Add("Entregada = @Entregada");

            if (desde.HasValue)
                condiciones.Add("FechaEntrega >= @Desde");

            if (hasta.HasValue)
                condiciones.Add("FechaEntrega <= @Hasta");

            string where =
                condiciones.Count > 0
                    ? " WHERE " + string.Join(" AND ", condiciones)
                    : "";

            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    "SELECT * FROM CredencialesImportadas" + where +
                    " ORDER BY Escuela, Apellidos, Nombre";

                using (SqliteCommand command =
                    new SqliteCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(escuela))
                        command.Parameters.AddWithValue("@Escuela", escuela);

                    if (entregada.HasValue)
                        command.Parameters.AddWithValue(
                            "@Entregada", entregada.Value ? 1 : 0);

                    if (desde.HasValue)
                        command.Parameters.AddWithValue(
                            "@Desde",
                            desde.Value.ToString("yyyy-MM-dd 00:00:00"));

                    if (hasta.HasValue)
                        command.Parameters.AddWithValue(
                            "@Hasta",
                            hasta.Value.ToString("yyyy-MM-dd 23:59:59"));

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            lista.Add(LeerCredencial(reader));
                    }
                }
            }

            return lista;
        }

        private CredencialImportada LeerCredencial(SqliteDataReader reader)
        {
            return new CredencialImportada
            {
                Id = Convert.ToInt32(reader["Id"]),
                Matricula = reader["Matricula"]?.ToString() ?? "",
                Nombre = reader["Nombre"]?.ToString() ?? "",
                Apellidos = reader["Apellidos"]?.ToString() ?? "",
                Vigencia = reader["Vigencia"]?.ToString() ?? "",
                Escuela = reader["Escuela"]?.ToString() ?? "",
                Area = reader["Area"]?.ToString() ?? "",
                Entregada = Convert.ToBoolean(reader["Entregada"]),
                FechaEntrega =
                    reader["FechaEntrega"] == DBNull.Value
                        ? ""
                        : reader["FechaEntrega"]?.ToString() ?? "",
                RutaFirma =
                    reader["RutaFirma"] == DBNull.Value
                        ? ""
                        : reader["RutaFirma"]?.ToString() ?? ""
            };
        }

        /// <summary>
        /// Lista de escuelas (nombres distintos) que existen en la base.
        /// </summary>
        public List<string> ObtenerEscuelas()
        {
            var lista = new List<string>();

            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"SELECT DISTINCT Escuela FROM CredencialesImportadas
                      WHERE Escuela IS NOT NULL AND Escuela <> ''
                      ORDER BY Escuela";

                using (SqliteCommand command =
                    new SqliteCommand(query, connection))
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        lista.Add(reader["Escuela"]?.ToString() ?? "");
                }
            }

            return lista;
        }

        // ----------------------------------------------------------------
        //  ENTREGAS Y FIRMAS
        // ----------------------------------------------------------------

        /// <summary>
        /// Elimina todas las credenciales de una escuela y borra los archivos de
        /// firma asociados. Devuelve cuántas credenciales se eliminaron.
        /// </summary>
        public int EliminarEscuela(string escuela)
        {
            int eliminadas = 0;

            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                // Borrar primero los PNG de firma de esa escuela.
                using (SqliteCommand firmas =
                    new SqliteCommand(
                        @"SELECT RutaFirma FROM CredencialesImportadas
                          WHERE Escuela = @Escuela
                            AND RutaFirma IS NOT NULL AND RutaFirma <> ''",
                        connection))
                {
                    firmas.Parameters.AddWithValue("@Escuela", escuela);

                    using (SqliteDataReader reader = firmas.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ruta = reader["RutaFirma"]?.ToString() ?? "";

                            try
                            {
                                if (ruta != "" && System.IO.File.Exists(ruta))
                                    System.IO.File.Delete(ruta);
                            }
                            catch
                            {
                                // Si un archivo no se puede borrar, se ignora.
                            }
                        }
                    }
                }

                using (SqliteCommand borrar =
                    new SqliteCommand(
                        "DELETE FROM CredencialesImportadas WHERE Escuela = @Escuela",
                        connection))
                {
                    borrar.Parameters.AddWithValue("@Escuela", escuela);
                    eliminadas = borrar.ExecuteNonQuery();
                }
            }

            return eliminadas;
        }

        /// <summary>
        /// Marca como ENTREGADAS (con fecha de hoy y sin firma) todas las
        /// credenciales pendientes. Si se indica escuela, solo las de esa escuela.
        /// Pensado para datos antiguos ya entregados antes de usar el programa.
        /// Devuelve cuántas se marcaron.
        /// </summary>
        public int MarcarTodasEntregadas(string? escuela)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"UPDATE CredencialesImportadas
                      SET Entregada = 1, FechaEntrega = @Fecha
                      WHERE Entregada = 0";

                if (!string.IsNullOrEmpty(escuela))
                    query += " AND Escuela = @Escuela";

                using (SqliteCommand command =
                    new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@Fecha", DateTime.Now.ToString(FormatoFecha));

                    if (!string.IsNullOrEmpty(escuela))
                        command.Parameters.AddWithValue("@Escuela", escuela);

                    return command.ExecuteNonQuery();
                }
            }
        }

        public void CambiarEstadoEntrega(int id, bool entregada)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"UPDATE CredencialesImportadas
                      SET Entregada = @Entregada,
                          FechaEntrega = @FechaEntrega
                      WHERE Id = @Id";

                using (SqliteCommand command =
                    new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@Entregada", entregada ? 1 : 0);

                    command.Parameters.AddWithValue(
                        "@FechaEntrega",
                        entregada
                            ? (object)DateTime.Now.ToString(FormatoFecha)
                            : DBNull.Value);

                    command.Parameters.AddWithValue("@Id", id);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void GuardarRutaFirma(int id, string rutaFirma)
        {
            EjecutarActualizacion(
                @"UPDATE CredencialesImportadas
                  SET RutaFirma = @RutaFirma WHERE Id = @Id",
                ("@RutaFirma", rutaFirma),
                ("@Id", id));
        }

        public string ObtenerRutaFirma(int id)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                string query =
                    @"SELECT RutaFirma FROM CredencialesImportadas WHERE Id = @Id";

                using (SqliteCommand command =
                    new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    object? resultado = command.ExecuteScalar();

                    return resultado == null || resultado == DBNull.Value
                        ? ""
                        : resultado.ToString() ?? "";
                }
            }
        }

        public void LimpiarRutaFirma(int id)
        {
            EjecutarActualizacion(
                @"UPDATE CredencialesImportadas
                  SET RutaFirma = NULL WHERE Id = @Id",
                ("@Id", id));
        }

        private void EjecutarActualizacion(
            string query,
            params (string nombre, object valor)[] parametros)
        {
            using (SqliteConnection connection =
                sqlite.ObtenerConexion())
            {
                connection.Open();

                using (SqliteCommand command =
                    new SqliteCommand(query, connection))
                {
                    foreach (var (nombre, valor) in parametros)
                        command.Parameters.AddWithValue(nombre, valor);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
