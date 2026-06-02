using iTextSharp.text;
using iTextSharp.text.pdf;
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
