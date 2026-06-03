using System.Collections.Generic;

namespace SistemaCredenciales.Models
{
    /// <summary>
    /// Un campo del diseño de la credencial (texto o foto) con su posición y
    /// estilo, en píxeles de la plantilla.
    /// </summary>
    public class CampoCredencial
    {
        /// <summary>"text" o "photo".</summary>
        public string Kind { get; set; } = "text";

        /// <summary>
        /// Dato a mostrar: MATRICULA, NOMBRE, APELLIDOS, CARRERA, VIGENCIA,
        /// CATEGORIA, NOMBRE_COMPLETO. Si está vacío, se usa <see cref="Texto"/>.
        /// </summary>
        public string Clave { get; set; } = "";

        /// <summary>Texto fijo (cuando no hay Clave).</summary>
        public string Texto { get; set; } = "";

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 200;
        public double Height { get; set; } = 60;

        public double FontSize { get; set; } = 22;
        public string Color { get; set; } = "#000000";
        public string Align { get; set; } = "left"; // left | center | right
        public bool Bold { get; set; }
        public double Rot { get; set; }
        public int Layer { get; set; }

        /// <summary>Etiqueta visible en el diseñador.</summary>
        public string Etiqueta { get; set; } = "";
    }

    /// <summary>Diseño de un lado de la credencial para un tipo de persona.</summary>
    public class DisenoCredencial
    {
        public string Tipo { get; set; } = "Alumno";   // Alumno | Docente | Admin
        public string Lado { get; set; } = "frente";   // frente | reverso
        public string PlantillaRuta { get; set; } = "";
        public List<CampoCredencial> Campos { get; set; } = new List<CampoCredencial>();
    }
}
