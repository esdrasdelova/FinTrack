using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using FinTrack.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinTrack.Services;

public interface IExportService
{
    byte[] TransactionsCsv(IEnumerable<Transaction> transactions);
    byte[] TransactionsExcel(IEnumerable<Transaction> transactions);
    byte[] ReportCsv(ReportDataDto report);
    byte[] ReportPdf(ReportDataDto report, string userFullName);
}

public class ExportService : IExportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] TransactionsCsv(IEnumerable<Transaction> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine('\uFEFF' + "Data;Tipo;Descrição;Categoria;Conta;Status;Valor;Observações");
        foreach (var t in transactions)
        {
            sb.AppendLine(string.Join(';',
                Escape(t.Date.ToString("dd/MM/yyyy")),
                Escape(t.Type == TransactionType.Income ? "Receita" : "Despesa"),
                Escape(t.Description),
                Escape(t.Category?.Name ?? string.Empty),
                Escape(t.Account?.Name ?? string.Empty),
                Escape(StatusName(t.Status)),
                t.Amount.ToString("0.00", PtBr),
                Escape(t.Notes ?? string.Empty)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] TransactionsExcel(IEnumerable<Transaction> transactions)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Transações");

        var headers = new[] { "Data", "Tipo", "Descrição", "Categoria", "Conta", "Status", "Valor", "Observações" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var t in transactions)
        {
            sheet.Cell(row, 1).Value = t.Date;
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Cell(row, 2).Value = t.Type == TransactionType.Income ? "Receita" : "Despesa";
            sheet.Cell(row, 3).Value = t.Description;
            sheet.Cell(row, 4).Value = t.Category?.Name ?? string.Empty;
            sheet.Cell(row, 5).Value = t.Account?.Name ?? string.Empty;
            sheet.Cell(row, 6).Value = StatusName(t.Status);
            sheet.Cell(row, 7).Value = (double)t.Amount;
            sheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, 8).Value = t.Notes ?? string.Empty;

            var color = t.Type == TransactionType.Income ? "10B981" : "F43F5E";
            sheet.Cell(row, 2).Style.Font.FontColor = XLColor.FromHtml("#" + color);
            row++;
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 26;

        sheet.RangeUsed().SetAutoFilter();
        sheet.Columns().AdjustToContents();
        sheet.Column(3).Width = 36;
        sheet.Column(8).Width = 44;
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ReportCsv(ReportDataDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine('\uFEFF' + $"Relatório;{Escape(report.Title)}");
        sb.AppendLine($"Período;{report.Labels.FirstOrDefault() ?? ""} a {report.Labels.LastOrDefault() ?? ""}");
        sb.AppendLine();

        sb.AppendLine("Série;" + string.Join(';', report.Labels.Select(Escape)));
        foreach (var series in report.Series)
            sb.AppendLine(Escape(series.Name) + ";" + string.Join(';', series.Data.Select(v => v.ToString("0.00", PtBr))));

        sb.AppendLine();
        sb.AppendLine("Data;Tipo;Descrição;Categoria;Conta;Status;Valor");
        foreach (var d in report.Details)
        {
            sb.AppendLine(string.Join(';',
                d.Date.ToString("dd/MM/yyyy"),
                d.Type == TransactionType.Income ? "Receita" : "Despesa",
                Escape(d.Description),
                Escape(d.CategoryName),
                Escape(d.AccountName),
                StatusName(d.Status),
                d.Amount.ToString("0.00", PtBr)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ReportPdf(ReportDataDto report, string userFullName)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor("#334155"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FINTRACK").Bold().FontSize(19).FontColor("#4F46E5");
                        col.Item().Text("Relatório financeiro gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", PtBr))
                            .FontSize(8).FontColor("#94A3B8");
                    });
                    row.ConstantItem(140).AlignRight().Text($"Emitido para\n{userFullName}").FontSize(8).FontColor("#64748B");
                });

                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Item().Background("#F1F5F9").Padding(12).Column(header =>
                    {
                        header.Item().Text(report.Title).Bold().FontSize(16).FontColor("#0F172A");
                        header.Item().Text("Período: " + string.Join(" · ", report.Labels.Take(2)) +
                                           (report.Labels.Count > 2 ? " … " + report.Labels[^1] : string.Empty))
                            .FontSize(9).FontColor("#64748B");
                        header.Item().Text("Total: " + report.Total.ToString("C", PtBr))
                            .Bold().FontSize(12).FontColor("#0F172A");
                    });

                    column.Item().PaddingTop(12).Text("Resumo por série").Bold().FontSize(11).FontColor("#0F172A");
                    column.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            for (var i = 0; i < report.Labels.Count && i < 12; i++)
                                c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Série").Bold();
                            foreach (var label in report.Labels.Take(12))
                                h.Cell().Text(label).Bold();
                        });

                        foreach (var series in report.Series)
                        {
                            table.Cell().Text(series.Name);
                            foreach (var value in series.Data.Take(12))
                                table.Cell().Text(value.ToString("N2", PtBr));
                        }
                    });

                    if (report.Details.Count > 0)
                    {
                        column.Item().PaddingTop(16).Text("Detalhamento (últimos 40 registros)").Bold().FontSize(11).FontColor("#0F172A");
                        column.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(58);
                                c.ConstantColumn(50);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.ConstantColumn(62);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Data").Bold();
                                h.Cell().Text("Tipo").Bold();
                                h.Cell().Text("Descrição").Bold();
                                h.Cell().Text("Categoria").Bold();
                                h.Cell().Text("Valor").Bold().AlignRight();
                            });

                            foreach (var d in report.Details.Take(40))
                            {
                                table.Cell().Text(d.Date.ToString("dd/MM/yyyy"));
                                table.Cell().Text(d.Type == TransactionType.Income ? "Entrada" : "Saída")
                                    .FontColor(d.Type == TransactionType.Income ? "#059669" : "#E11D48");
                                table.Cell().Text(d.Description);
                                table.Cell().Text(d.CategoryName);
                                table.Cell().Text(d.Amount.ToString("N2", PtBr)).AlignRight();
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Row(row =>
                {
                    row.RelativeItem().Text("FinTrack · Gestão financeira profissional").FontSize(7).FontColor("#94A3B8");
                    row.ConstantItem(60).AlignRight().Text(x =>
                    {
                        x.Span("Página ").FontSize(7).FontColor("#94A3B8");
                        x.CurrentPageNumber().FontSize(7).FontColor("#94A3B8");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string StatusName(TransactionStatus status) => status switch
    {
        TransactionStatus.Completed => "Concluída",
        TransactionStatus.Pending => "Pendente",
        _ => "Cancelada"
    };

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains(';') || escaped.Contains('"') || escaped.Contains('\n')
            ? $"\"{escaped}\""
            : escaped;
    }
}
