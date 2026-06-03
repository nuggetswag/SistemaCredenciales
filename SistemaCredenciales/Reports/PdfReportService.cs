using iTextSharp.text;
using iTextSharp.text.pdf;
using SistemaCredenciales.Models;
using SistemaCredenciales.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace SistemaCredenciales.Reports
{
    public class PdfReportService
    {
        private static readonly Font FuenteTitulo =
            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);

        private static readonly Font FuenteSubtitulo =
            FontFactory.GetFont(FontFactory.HELVETICA, 12, new BaseColor(90, 90, 90));

        private static readonly Font FuenteEncabezadoTabla =
            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(255, 255, 255));

        private static readonly Font FuenteCelda =
            FontFactory.GetFont(FontFactory.HELVETICA, 9);

        /// <summary>
        /// Genera un PDF de tabla genérico y devuelve la ruta del archivo creado.
        /// </summary>
        public string GenerarReporte(
            string titulo,
            string subtitulo,
            string[] encabezados,
            List<string[]> filas,
            string rutaSalida)
        {
            Document documento = new Document(PageSize.A4, 36, 36, 40, 40);

            using (FileStream stream =
                new FileStream(rutaSalida, FileMode.Create))
            {
                PdfWriter.GetInstance(documento, stream);

                documento.Open();

                EscribirEncabezado(documento, titulo, subtitulo);

                PdfPTable tabla =
                    new PdfPTable(encabezados.Length)
                    {
                        WidthPercentage = 100
                    };

                foreach (string encabezado in encabezados)
                {
                    PdfPCell celda =
                        new PdfPCell(new Phrase(encabezado, FuenteEncabezadoTabla))
                        {
                            BackgroundColor = new BaseColor(30, 41, 59),
                            Padding = 5
                        };

                    tabla.AddCell(celda);
                }

                foreach (string[] fila in filas)
                {
                    foreach (string valor in fila)
                    {
                        tabla.AddCell(
                            new PdfPCell(new Phrase(valor ?? "", FuenteCelda))
                            {
                                Padding = 4
                            });
                    }
                }

                documento.Add(tabla);

                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph(
                    $"Total de registros: {filas.Count}", FuenteSubtitulo));

                documento.Close();
            }

            return rutaSalida;
        }

        /// <summary>
        /// Reporte de entregas que muestra la IMAGEN de la firma en cada fila
        /// (en vez de un "sí/no"). Devuelve la ruta del PDF generado.
        /// </summary>
        public string GenerarReporteEntregas(
            List<CredencialImportada> lista,
            string titulo,
            string subtitulo,
            string rutaSalida)
        {
            Document documento = new Document(PageSize.A4, 36, 36, 40, 40);

            using (FileStream stream =
                new FileStream(rutaSalida, FileMode.Create))
            {
                PdfWriter.GetInstance(documento, stream);
                documento.Open();

                EscribirEncabezado(documento, titulo, subtitulo);

                var tabla = new PdfPTable(6) { WidthPercentage = 100 };
                tabla.SetWidths(new float[] { 1.1f, 1.6f, 1.8f, 1.5f, 1.3f, 1.6f });

                foreach (string encabezado in new[]
                    { "Matrícula", "Nombre", "Apellidos", "Escuela", "Fecha", "Firma" })
                {
                    tabla.AddCell(new PdfPCell(
                        new Phrase(encabezado, FuenteEncabezadoTabla))
                    {
                        BackgroundColor = new BaseColor(30, 41, 59),
                        Padding = 5
                    });
                }

                foreach (CredencialImportada c in lista)
                {
                    tabla.AddCell(Celda(c.Matricula));
                    tabla.AddCell(Celda(c.Nombre));
                    tabla.AddCell(Celda(c.Apellidos));
                    tabla.AddCell(Celda(c.Escuela));
                    tabla.AddCell(Celda(c.FechaEntrega));
                    tabla.AddCell(CeldaFirma(c.RutaFirma));
                }

                documento.Add(tabla);
                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph(
                    $"Total de entregas: {lista.Count}", FuenteSubtitulo));

                documento.Close();
            }

            return rutaSalida;
        }

        /// <summary>
        /// Reporte de credenciales con columnas configurables. Si se incluye la
        /// firma, muestra la IMAGEN de la firma en cada fila.
        /// </summary>
        public string GenerarReporteCredenciales(
            List<CredencialImportada> datos,
            string titulo,
            string subtitulo,
            string rutaSalida,
            bool incluirFecha,
            bool incluirFirma)
        {
            var encabezados = new List<string>
                { "Matrícula", "Nombre", "Apellidos", "Escuela", "Vigencia", "Estado" };
            var anchos = new List<float> { 1.0f, 1.4f, 1.6f, 1.4f, 1.0f, 1.0f };

            if (incluirFecha) { encabezados.Add("Fecha"); anchos.Add(1.2f); }
            if (incluirFirma) { encabezados.Add("Firma"); anchos.Add(1.7f); }

            Document documento = new Document(PageSize.A4, 36, 36, 40, 40);

            using (FileStream stream =
                new FileStream(rutaSalida, FileMode.Create))
            {
                PdfWriter.GetInstance(documento, stream);
                documento.Open();

                EscribirEncabezado(documento, titulo, subtitulo);

                var tabla = new PdfPTable(encabezados.Count) { WidthPercentage = 100 };
                tabla.SetWidths(anchos.ToArray());

                foreach (string encabezado in encabezados)
                {
                    tabla.AddCell(new PdfPCell(
                        new Phrase(encabezado, FuenteEncabezadoTabla))
                    {
                        BackgroundColor = new BaseColor(30, 41, 59),
                        Padding = 5
                    });
                }

                foreach (CredencialImportada c in datos)
                {
                    tabla.AddCell(Celda(c.Matricula));
                    tabla.AddCell(Celda(c.Nombre));
                    tabla.AddCell(Celda(c.Apellidos));
                    tabla.AddCell(Celda(c.Escuela));
                    tabla.AddCell(Celda(c.Vigencia));
                    tabla.AddCell(Celda(c.Entregada ? "ENTREGADA" : "PENDIENTE"));

                    if (incluirFecha)
                        tabla.AddCell(Celda(c.FechaEntrega));

                    if (incluirFirma)
                        tabla.AddCell(CeldaFirma(c.RutaFirma));
                }

                documento.Add(tabla);
                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph(
                    $"Total de registros: {datos.Count}", FuenteSubtitulo));

                documento.Close();
            }

            return rutaSalida;
        }

        /// <summary>
        /// Reporte de fotos: una cuadrícula con la imagen de cada persona, su
        /// matrícula y su nombre. Las fotos vienen de la base .mdb (IDWFOTO).
        /// </summary>
        public string GenerarReporteFotos(
            List<FotoCredencial> fotos,
            string titulo,
            string subtitulo,
            string rutaSalida)
        {
            var fuenteMatricula =
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var fuenteNombre =
                FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(71, 85, 105));

            Document documento = new Document(PageSize.A4, 24, 24, 36, 30);

            using (FileStream stream =
                new FileStream(rutaSalida, FileMode.Create))
            {
                PdfWriter.GetInstance(documento, stream);
                documento.Open();

                EscribirEncabezado(documento, titulo, subtitulo);

                const int columnas = 4;
                var tabla = new PdfPTable(columnas) { WidthPercentage = 100 };

                foreach (FotoCredencial f in fotos)
                {
                    var celda = new PdfPCell
                    {
                        Padding = 8,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BorderColor = new BaseColor(226, 232, 240),
                        BorderWidth = 1f
                    };

                    try
                    {
                        Image imagen = Image.GetInstance(f.Foto);
                        imagen.ScaleToFit(95f, 115f);
                        imagen.Alignment = Element.ALIGN_CENTER;
                        celda.AddElement(imagen);
                    }
                    catch
                    {
                        celda.AddElement(new Paragraph("(sin imagen)", fuenteNombre));
                    }

                    celda.AddElement(new Paragraph(f.Matricula, fuenteMatricula)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 5
                    });

                    if (!string.IsNullOrWhiteSpace(f.Nombre))
                    {
                        celda.AddElement(new Paragraph(f.Nombre, fuenteNombre)
                        {
                            Alignment = Element.ALIGN_CENTER
                        });
                    }

                    tabla.AddCell(celda);
                }

                // Completar la última fila para que la cuadrícula quede pareja.
                int resto = fotos.Count % columnas;
                if (resto > 0)
                {
                    for (int i = resto; i < columnas; i++)
                        tabla.AddCell(new PdfPCell { Border = Rectangle.NO_BORDER });
                }

                documento.Add(tabla);
                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph(
                    $"Total de fotos: {fotos.Count}", FuenteSubtitulo));

                documento.Close();
            }

            return rutaSalida;
        }

        /// <summary>
        /// Comprobante individual de entrega: foto, datos y firma de recibido.
        /// </summary>
        public string GenerarComprobante(
            CredencialImportada c,
            byte[]? foto,
            string rutaFirma,
            string rutaSalida)
        {
            var fLabel = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var fVal = FontFactory.GetFont(FontFactory.HELVETICA, 11);

            Document documento = new Document(PageSize.A4, 45, 45, 45, 45);

            using (FileStream stream =
                new FileStream(rutaSalida, FileMode.Create))
            {
                PdfWriter.GetInstance(documento, stream);
                documento.Open();

                EscribirEncabezado(documento,
                    "Comprobante de entrega de credencial", "");

                var tabla = new PdfPTable(2) { WidthPercentage = 100 };
                tabla.SetWidths(new float[] { 1f, 2.2f });

                var celdaFoto = new PdfPCell
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 6
                };

                bool fotoPuesta = false;
                if (foto != null && foto.Length > 100)
                {
                    try
                    {
                        Image img = Image.GetInstance(foto);
                        img.ScaleToFit(130f, 165f);
                        img.Alignment = Element.ALIGN_CENTER;
                        celdaFoto.AddElement(img);
                        fotoPuesta = true;
                    }
                    catch { }
                }
                if (!fotoPuesta)
                    celdaFoto.AddElement(new Paragraph("(sin foto)", fVal));

                tabla.AddCell(celdaFoto);

                var celdaDatos = new PdfPCell
                {
                    Border = Rectangle.NO_BORDER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 6
                };

                void Linea(string etiqueta, string valor)
                {
                    var p = new Paragraph { SpacingAfter = 7 };
                    p.Add(new Chunk(etiqueta + ": ", fLabel));
                    p.Add(new Chunk(valor ?? "", fVal));
                    celdaDatos.AddElement(p);
                }

                Linea("Matrícula", c.Matricula);
                Linea("Nombre", (c.Nombre + " " + c.Apellidos).Trim());
                Linea("Escuela", c.Escuela);
                Linea("Vigencia", c.Vigencia);
                Linea("Fecha de entrega", c.FechaEntrega);

                tabla.AddCell(celdaDatos);
                documento.Add(tabla);

                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph("Firma de recibido:", fLabel));

                if (!string.IsNullOrEmpty(rutaFirma) && File.Exists(rutaFirma))
                {
                    try
                    {
                        Image firma = Image.GetInstance(rutaFirma);
                        firma.ScaleToFit(240f, 100f);
                        firma.SpacingBefore = 6;
                        documento.Add(firma);
                    }
                    catch { }
                }
                else
                {
                    documento.Add(new Paragraph("(sin firma)", fVal));
                }

                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph(
                    "Recibí mi credencial escolar de conformidad.", FuenteSubtitulo));

                documento.Close();
            }

            return rutaSalida;
        }

        private PdfPCell Celda(string texto)
        {
            return new PdfPCell(new Phrase(texto ?? "", FuenteCelda))
            {
                Padding = 4,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
        }

        private PdfPCell CeldaFirma(string rutaFirma)
        {
            if (!string.IsNullOrEmpty(rutaFirma) && File.Exists(rutaFirma))
            {
                try
                {
                    Image imagen = Image.GetInstance(rutaFirma);
                    imagen.ScaleToFit(110f, 45f);

                    return new PdfPCell(imagen, false)
                    {
                        Padding = 3,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                }
                catch
                {
                    // Si la imagen no se puede leer, se muestra texto.
                }
            }

            return new PdfPCell(new Phrase("Sin firma", FuenteCelda))
            {
                Padding = 4,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
        }

        private void EscribirEncabezado(
            Document documento,
            string titulo,
            string subtitulo)
        {
            Paragraph institucion =
                new Paragraph(
                    AppConfig.Actual.NombreInstitucion,
                    FuenteSubtitulo)
                {
                    Alignment = Element.ALIGN_CENTER
                };

            documento.Add(institucion);

            Paragraph parrafoTitulo =
                new Paragraph(titulo, FuenteTitulo)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 4
                };

            documento.Add(parrafoTitulo);

            if (!string.IsNullOrWhiteSpace(subtitulo))
            {
                documento.Add(new Paragraph(subtitulo, FuenteSubtitulo)
                {
                    Alignment = Element.ALIGN_CENTER
                });
            }

            documento.Add(new Paragraph(
                $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                FuenteSubtitulo)
            {
                Alignment = Element.ALIGN_CENTER
            });

            documento.Add(new Paragraph(" "));
        }
    }
}
