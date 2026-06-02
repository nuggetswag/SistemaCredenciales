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
