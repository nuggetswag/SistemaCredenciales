# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> El usuario se comunica en español y no es programador. Explica el "porqué" en español
> y prefiere que implementes de principio a fin, consultando solo en casos extremos.

## Comandos

```powershell
# Compilar
dotnet build SistemaCredenciales.slnx

# Ejecutar
dotnet run --project SistemaCredenciales/SistemaCredenciales.csproj

# Release
dotnet build SistemaCredenciales.slnx -c Release
```

No hay pruebas automatizadas. La app es WPF (interfaz gráfica), así que la verificación
final es manual: ejecutar y probar los flujos.

**Importante al compilar:** si la app está abierta, el `.exe` queda bloqueado y el build
falla con `MSB3021/MSB3027` (no es un error de código). Cierra la app antes:
`Get-Process SistemaCredenciales | Stop-Process -Force`.

## Arquitectura

App de escritorio **WPF (.NET 10, solo Windows)** para controlar la entrega de credenciales
escolares. Diseñada para ser **portable**: todo se guarda junto al `.exe`.

### Flujo de datos
1. **Origen externo**: cada escuela es un archivo `.mdb` (Access) o `.xlsx` (Excel) en el
   escritorio. Se leen con el proveedor OLE DB `Microsoft.ACE.OLEDB.12.0`.
2. **Almacén local**: una sola base **SQLite** (`SistemaCredenciales.db`) junto al `.exe`,
   creada/migrada en `SQLiteService`. Tabla única `CredencialesImportadas`.
3. **Identidad de "escuela"**: es el **nombre del archivo de origen**, guardado en la columna
   `Escuela`. Casi todo se filtra por ese campo (`ObtenerCredenciales(escuela)`,
   `ObtenerCredencialesFiltradas(...)`).
4. **Firmas**: al entregar, `FirmaWindow` captura la firma en un `InkCanvas`, la guarda como
   PNG en la carpeta de firmas y escribe la ruta en SQLite.
5. **Salidas**: reportes en PDF (iTextSharp) y exportaciones en CSV, ambos a la carpeta de
   reportes.

### Capas / archivos clave
| Archivo | Rol |
|---|---|
| `Services/AppConfig.cs` | Configuración portable en `config.json` (carpeta de búsqueda, firmas, reportes, nombre de institución). Acceso global vía `AppConfig.Actual`. |
| `Services/SQLiteService.cs` | Crea y **migra** el esquema en cada arranque (`ALTER TABLE` por columna si falta, backfill de `Escuela`). |
| `Services/DatabaseService.cs` | Todo el SQL + importación de Access/Excel (auto-detección de columnas) + consultas filtradas. Depende de `SQLiteService`. |
| `Services/ExportService.cs` | Exportación a CSV compatible con Excel (UTF-8 con BOM, `;`). |
| `Reports/PdfReportService.cs` | Generador de PDF de tabla genérico (`GenerarReporte(titulo, subtitulo, encabezados, filas, ruta)`). |
| `Models/CredencialImportada.cs` | Modelo único; mapea 1-a-1 con la tabla SQLite. |

### Ventanas
- `MainWindow`: tablero + tabla. Mantiene `escuelaActual` (filtro activo) y un ComboBox de
  escuelas. Singleton `MainWindow.Instancia` para que otras ventanas la recarguen.
- `EscuelasWindow`: muestra las escuelas (archivos) encontradas; al elegir una, importa y
  fija el filtro a esa escuela vía `MainWindow.Instancia.SeleccionarEscuela(...)`.
- `LotesWindow`: importar masivo (Excel/Access) y exportar CSV.
- `EntregasWindow`: entregas por escuela, con fecha.
- `Reports/ReportesWindow`: menú de escenarios de reporte (clase en namespace
  `SistemaCredenciales`, no `.Reports`, porque su `x:Class` lo define así).
- `ConfiguracionWindow`: edita `AppConfig` y lo guarda.

## Convenciones y notas
- **Portabilidad**: nunca uses rutas absolutas. Las rutas salen de `AppConfig.Actual`
  (que por defecto resuelve a `AppDomain.CurrentDomain.BaseDirectory` y al escritorio).
- **Nullable está activado** (`<Nullable>enable</Nullable>`): mantén el build en 0 warnings.
  Patrón usado: `reader["x"]?.ToString() ?? ""`, propiedades string inicializadas a `""`.
- **Dependencia externa**: importar `.mdb`/`.xlsx` requiere el **Microsoft Access Database
  Engine** instalado en la máquina (no es paquete NuGet). Sin él, la importación truena.
- **Importación de Excel**: detecta las columnas por nombre (sin acentos ni mayúsculas) en
  `DatabaseService.DetectarColumnas`; agrega alias ahí si llegan encabezados nuevos.
- **git/gh**: el usuario es principiante. Explica los pasos; antes se creía que git no estaba
  disponible para el agente, pero sí lo está en este entorno.
