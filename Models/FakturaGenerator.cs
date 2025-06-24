using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;
using QuestPDF.Previewer;

namespace JitkaApp.Models
{
    public class FakturaGenerator
    {
        public static byte[] VytvorFakturu(string cisloFaktury, string zakaznikJmeno, string zakaznikAdresa, List<(string nazev, int mnozstvi, decimal cena)> polozky)
        {
            decimal celkem = polozky.Sum(p => p.mnozstvi * p.cena);

            var dokument = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Row(row =>
                    {
                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("JitkaApp s.r.o.").Bold().FontSize(16);
                            col.Item().Text("Ulička 123, 123 45 Město");
                            col.Item().Text("IČO: 12345678 | DIČ: CZ12345678");
                        });

                        row.ConstantColumn(200).Column(col =>
                        {
                            col.Item().Text($"Faktura č. {cisloFaktury}").Bold().FontSize(14);
                            col.Item().Text($"Datum vystavení: {DateTime.Now:dd.MM.yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("Odběratel").Bold();
                        col.Item().Text(zakaznikJmeno);
                        col.Item().Text(zakaznikAdresa);

                        col.Item().LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Název
                                columns.RelativeColumn(1); // Množství
                                columns.RelativeColumn(2); // Cena
                                columns.RelativeColumn(2); // Celkem
                            });

                            // Hlavička
                            table.Header(header =>
                            {
                                header.Cell().Text("Položka").Bold();
                                header.Cell().AlignRight().Text("Množství").Bold();
                                header.Cell().AlignRight().Text("Cena za ks").Bold();
                                header.Cell().AlignRight().Text("Celkem").Bold();
                            });

                            foreach (var p in polozky)
                            {
                                table.Cell().Text(p.nazev);
                                table.Cell().AlignRight().Text(p.mnozstvi.ToString());
                                table.Cell().AlignRight().Text($"{p.cena:N2} Kč");
                                table.Cell().AlignRight().Text($"{(p.cena * p.mnozstvi):N2} Kč");
                            }

                            // Součet
                            table.Cell().ColumnSpan(3).AlignRight().Text("Celkem").Bold();
                            table.Cell().AlignRight().Text($"{celkem:N2} Kč").Bold();
                        });

                        col.Item().LineHorizontal(1);
                        col.Item().Text("Děkujeme za objednávku!").Italic();
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Vygenerováno aplikací JitkaApp");
                    });
                });
            });

            return dokument.GeneratePdf();
        }
    }
}
