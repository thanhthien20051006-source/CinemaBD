using CinemaBD.Web.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CinemaBD.Web.Infrastructure.Notifications;

public interface ITicketPdfService
{
    byte[] CreateInvoicePdf(InvoiceViewModel invoice);
}

public class TicketPdfService : ITicketPdfService
{
    public TicketPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] CreateInvoicePdf(InvoiceViewModel invoice)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Background(Colors.Blue.Darken3).Padding(18).Column(col =>
                {
                    col.Item().Text("CINEMABD - VE DIEN TU").FontColor(Colors.White).Bold().FontSize(20);
                    col.Item().Text($"Ma giao dich: {invoice.TransactionRef}").FontColor(Colors.White);
                });

                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Spacing(14);
                    col.Item().Text("Thong tin dat ve").Bold().FontSize(16);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                        Row(table, "Phim", invoice.MovieTitle);
                        Row(table, "Ngay chieu", invoice.ShowDate?.ToString("dd/MM/yyyy") ?? "-");
                        Row(table, "Gio", invoice.StartTime?.ToString(@"hh\:mm") ?? "-");
                        Row(table, "Phong", invoice.RoomName);
                        Row(table, "Ghe", invoice.Seats.Any() ? string.Join(", ", invoice.Seats) : "-");
                        Row(table, "Tong tien", $"{invoice.TotalAmount:N0} VND");
                        Row(table, "Trang thai", invoice.PaymentStatus);
                    });

                    col.Item().Text("QR check-in tung ve").Bold().FontSize(16);
                    var tickets = invoice.Tickets.Any()
                        ? invoice.Tickets
                        : invoice.Seats.Select(seat => new InvoiceTicketViewModel { SeatId = seat, TicketId = seat }).ToList();
                    foreach (var ticket in tickets)
                    {
                        var ticketToken = string.IsNullOrWhiteSpace(ticket.TicketId) ? ticket.SeatId : ticket.TicketId;
                        var payload = $"CinemaBD|CHECKIN|{invoice.TransactionRef}|{invoice.PaymentId}|{ticketToken}";
                        var qrBytes = CreateQrPng(payload);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Ghe: {ticket.SeatId}").Bold().FontSize(14);
                                c.Item().Text($"Ma ve: {ticket.TicketId}").FontSize(10);
                                c.Item().Text($"Payload: {payload}").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(110).Image(qrBytes);
                        });
                    }
                });

                page.Footer().AlignCenter().Text("Vui long xuat trinh QR tai quay soat ve. Moi QR chi check-in duoc mot lan.").FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }

    private static void Row(TableDescriptor table, string label, string? value)
    {
        table.Cell().PaddingVertical(4).Text(label).Bold();
        table.Cell().PaddingVertical(4).Text(value ?? "-");
    }

    private static byte[] CreateQrPng(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(qrData).GetGraphic(12);
    }
}
