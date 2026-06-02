using iTextSharp.text;
using iTextSharp.text.pdf;
using SistemaCredenciales.Models;
using System.Collections.Generic;
using System.IO;

namespace SistemaCredenciales.Reports
{
    public class PdfReportService
    {
        public void GenerarReporte(
            List<CredencialImportada> lista)
        {
            string ruta =
    Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "ReporteCredenciales.pdf");

            Document documento =
                new Document(PageSize.A4);

            PdfWriter.GetInstance(
                documento,
                new FileStream(
                    ruta,
                    FileMode.Create));

            documento.Open();

            Paragraph titulo =
                new Paragraph(
                    "REPORTE DE CREDENCIALES");

            titulo.Alignment =
                Element.ALIGN_CENTER;

            documento.Add(titulo);

            documento.Add(new Paragraph(" "));

            PdfPTable tabla =
                new PdfPTable(5);

            tabla.WidthPercentage = 100;

            tabla.AddCell("Matrícula");
            tabla.AddCell("Nombre");
            tabla.AddCell("Apellidos");
            tabla.AddCell("Escuela");
            tabla.AddCell("Estado");

            foreach (var item in lista)
            {
                tabla.AddCell(item.Matricula);
                tabla.AddCell(item.Nombre);
                tabla.AddCell(item.Apellidos);
                tabla.AddCell(item.Escuela);

                tabla.AddCell(
                    item.Entregada
                    ? "ENTREGADA"
                    : "PENDIENTE");
            }

            documento.Add(tabla);

            documento.Close();
        }
    }
}