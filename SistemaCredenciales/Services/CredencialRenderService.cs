using SistemaCredenciales.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace SistemaCredenciales.Services
{
    /// <summary>
    /// Dibuja un lado de la credencial (plantilla + texto + foto) a partir de un
    /// diseño. Devuelve la imagen en PNG (bytes), lista para PDF o vista previa.
    /// </summary>
    public class CredencialRenderService
    {
        public const int DefaultW = 1013; // CR80 a ~300 dpi (horizontal)
        public const int DefaultH = 638;
        public const int NormLado = 1013; // lado largo máximo del lienzo de trabajo

        /// <summary>
        /// Tamaño del lienzo de trabajo a partir de la plantilla: se normaliza el
        /// lado largo a <see cref="NormLado"/> px (las plantillas suelen venir en
        /// resolución enorme). Si no hay plantilla, usa el tamaño por defecto.
        /// </summary>
        public static (int w, int h) TamanoCanvas(string plantilla)
        {
            if (!string.IsNullOrWhiteSpace(plantilla) && File.Exists(plantilla))
            {
                try
                {
                    using var ms = new MemoryStream(File.ReadAllBytes(plantilla));
                    using var img = Image.FromStream(ms);
                    double f = (double)NormLado / Math.Max(img.Width, img.Height);
                    if (f > 1) f = 1; // no agrandar plantillas pequeñas
                    return ((int)Math.Round(img.Width * f), (int)Math.Round(img.Height * f));
                }
                catch { }
            }
            return (DefaultW, DefaultH);
        }

        public byte[] RenderLado(
            CredencialImportada persona, DisenoCredencial diseno, byte[]? foto)
        {
            using Bitmap canvas = CrearLienzo(diseno.PlantillaRuta);

            using (var g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint =
                    System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                foreach (CampoCredencial c in diseno.Campos.OrderBy(k => k.Layer))
                {
                    if (c.Kind == "photo")
                        DibujarFoto(g, c, foto);
                    else
                        DibujarTexto(g, c, persona);
                }
            }

            using var ms = new MemoryStream();
            canvas.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        private Bitmap CrearLienzo(string plantilla)
        {
            var (w, h) = TamanoCanvas(plantilla);
            var canvas = new Bitmap(w, h);

            using (var g = Graphics.FromImage(canvas))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (!string.IsNullOrWhiteSpace(plantilla) && File.Exists(plantilla))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(plantilla);
                        using var ms = new MemoryStream(bytes);
                        using var tpl = Image.FromStream(ms);
                        g.DrawImage(tpl, new Rectangle(0, 0, w, h));
                        return canvas;
                    }
                    catch { }
                }

                g.Clear(Color.White);
            }
            return canvas;
        }

        private void DibujarTexto(Graphics g, CampoCredencial c, CredencialImportada p)
        {
            string txt = ResolverValor(c, p);
            if (string.IsNullOrEmpty(txt))
                return;

            using var font = new Font("Arial", (float)Math.Max(c.FontSize, 1),
                c.Bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(ParseColor(c.Color));

            var fmt = new StringFormat
            {
                Alignment = c.Align == "center" ? StringAlignment.Center
                          : c.Align == "right" ? StringAlignment.Far
                          : StringAlignment.Near
            };

            float w = (float)Math.Max(c.Width, 1);

            if (Math.Abs(c.Rot) > 0.01)
            {
                GraphicsState state = g.Save();
                g.TranslateTransform((float)c.X, (float)c.Y);
                g.RotateTransform((float)c.Rot);
                g.DrawString(txt, font, brush,
                    new RectangleF(0, 0, w, 100000f), fmt);
                g.Restore(state);
            }
            else
            {
                g.DrawString(txt, font, brush,
                    new RectangleF((float)c.X, (float)c.Y, w, 100000f), fmt);
            }
        }

        private void DibujarFoto(Graphics g, CampoCredencial c, byte[]? foto)
        {
            var dest = new Rectangle(
                (int)c.X, (int)c.Y,
                (int)Math.Max(c.Width, 1), (int)Math.Max(c.Height, 1));

            if (foto != null && foto.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(foto);
                    using var img = Image.FromStream(ms);

                    // cover: llenar el rectángulo sin deformar, recortando centrado
                    double escala = Math.Max(
                        (double)dest.Width / img.Width,
                        (double)dest.Height / img.Height);
                    int w = (int)(img.Width * escala);
                    int h = (int)(img.Height * escala);
                    int x = dest.X - (w - dest.Width) / 2;
                    int y = dest.Y - (h - dest.Height) / 2;

                    Region old = g.Clip;
                    g.SetClip(dest);
                    g.DrawImage(img, new Rectangle(x, y, w, h));
                    g.Clip = old;
                    return;
                }
                catch { }
            }

            using var pen = new Pen(Color.FromArgb(180, 180, 180), 2);
            g.DrawRectangle(pen, dest);
            using var f = new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel);
            g.DrawString("FOTO", f, Brushes.Gray, dest.X + 6, dest.Y + 6);
        }

        private string ResolverValor(CampoCredencial c, CredencialImportada p)
        {
            if (string.IsNullOrEmpty(c.Clave))
                return c.Texto ?? "";

            switch (c.Clave.ToUpperInvariant())
            {
                case "MATRICULA": return p.Matricula;
                case "NOMBRE": return p.Nombre;
                case "APELLIDOS": return p.Apellidos;
                case "NOMBRE_COMPLETO": return (p.Nombre + " " + p.Apellidos).Trim();
                case "CARRERA": return p.Carrera;
                case "VIGENCIA": return p.Vigencia;
                case "CATEGORIA": return p.Categoria;
                case "ESCUELA": return p.Escuela;
                default: return c.Texto ?? "";
            }
        }

        private Color ParseColor(string hex)
        {
            try { return ColorTranslator.FromHtml(hex); }
            catch { return Color.Black; }
        }
    }
}
