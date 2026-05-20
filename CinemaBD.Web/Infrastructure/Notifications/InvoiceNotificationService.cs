using System.Net;
using System.Net.Mail;
using System.Text;
using CinemaBD.Web.Configurations;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.Extensions.Options;
using QRCoder;

namespace CinemaBD.Web.Infrastructure.Notifications;

public interface IInvoiceNotificationService
{
    Task<bool> SendInvoiceAsync(string txnRef, CancellationToken cancellationToken = default);
}

public class InvoiceNotificationService : IInvoiceNotificationService
{
    private readonly CinemaApiClient _apiClient;
    private readonly EmailSettings _emailSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITicketPdfService _ticketPdfService;
    private readonly ILogger<InvoiceNotificationService> _logger;

    public InvoiceNotificationService(
        CinemaApiClient apiClient,
        IOptions<EmailSettings> emailOptions,
        IHttpContextAccessor httpContextAccessor,
        ITicketPdfService ticketPdfService,
        ILogger<InvoiceNotificationService> logger)
    {
        _apiClient = apiClient;
        _emailSettings = emailOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _ticketPdfService = ticketPdfService;
        _logger = logger;
    }

    public async Task<bool> SendInvoiceAsync(string txnRef, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(txnRef)) return false;

        var senderEmail = _emailSettings.SenderEmail;
        var senderPassword = _emailSettings.SenderPassword;

        if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
        {
            _logger.LogWarning("Skip invoice email {TxnRef}: email account/password is empty. Set EmailSettings FromEmail/FromPassword or SMTPAccount/SMTPPassword.", txnRef);
            return false;
        }

        var invoice = await _apiClient.GetInvoiceAsync(txnRef, cancellationToken);
        if (invoice == null)
        {
            _logger.LogWarning("Skip invoice email {TxnRef}: invoice not found.", txnRef);
            return false;
        }

        var receiver = await ResolveReceiverEmailAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(receiver))
        {
            _logger.LogWarning("Skip invoice email {TxnRef}: receiver email not found from current user profile.", txnRef);
            return false;
        }

        var qrPayload = $"CinemaBD|CHECKIN|{invoice.TransactionRef}|{invoice.PaymentId}|{string.Join(",", invoice.Seats)}";
        var qrPng = CreateQrPng(qrPayload);
        var pdfBytes = _ticketPdfService.CreateInvoicePdf(invoice);
        var body = BuildEmailBody(invoice);

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, _emailSettings.FromName),
            Subject = $"CinemaBD - Vé xem phim {invoice.TransactionRef}",
            Body = body,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        message.To.Add(receiver);
        message.Attachments.Add(new Attachment(new MemoryStream(qrPng), $"qr-{txnRef}.png", "image/png"));
        message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"ve-dien-tu-{txnRef}.pdf", "application/pdf"));

        using var smtp = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(senderEmail, senderPassword)
        };

        try
        {
            await smtp.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Invoice email {TxnRef} sent to {Receiver}.", txnRef, receiver);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice email {TxnRef} failed.", txnRef);
            return false;
        }
    }

    private async Task<string?> ResolveReceiverEmailAsync(CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token)) return null;

        var profile = await _apiClient.GetProfileAsync(token, cancellationToken);
        return string.IsNullOrWhiteSpace(profile?.Email) ? null : profile.Email;
    }

    private static byte[] CreateQrPng(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(qrData).GetGraphic(20);
    }

    private static string BuildEmailBody(InvoiceViewModel invoice)
    {
        var showDate = invoice.ShowDate?.ToString("dd/MM/yyyy") ?? "-";
        var showTime = invoice.StartTime?.ToString(@"hh\:mm") ?? "-";
        var seats = invoice.Seats.Any() ? string.Join(", ", invoice.Seats) : "-";
        var combos = invoice.Combos.Any()
            ? string.Join("", invoice.Combos.Select(x => $"<li>{WebUtility.HtmlEncode(x)}</li>"))
            : "<li>Không có dịch vụ đi kèm</li>";

        return $@"
<div style=""font-family:Arial,sans-serif;background:#f5f7fb;padding:24px;color:#111827"">
  <div style=""max-width:680px;margin:auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 10px 30px rgba(0,0,0,.08)"">
    <div style=""background:#16a34a;color:white;padding:24px 28px"">
      <h2 style=""margin:0"">Thanh toán thành công</h2>
      <p style=""margin:8px 0 0"">Cảm ơn bạn đã đặt vé tại CinemaBD.</p>
    </div>
    <div style=""padding:28px"">
      <p><b>Mã hóa đơn:</b> {WebUtility.HtmlEncode(invoice.TransactionRef)}</p>
      <p><b>Trạng thái:</b> {WebUtility.HtmlEncode(invoice.PaymentStatus)}</p>
      <p><b>Phim:</b> {WebUtility.HtmlEncode(invoice.MovieTitle)}</p>
      <p><b>Ngày chiếu:</b> {showDate} {showTime}</p>
      <p><b>Phòng:</b> {WebUtility.HtmlEncode(invoice.RoomName)}</p>
      <p><b>Ghế:</b> {WebUtility.HtmlEncode(seats)}</p>
      <p><b>Tổng tiền:</b> <span style=""color:#dc2626;font-weight:bold"">{invoice.TotalAmount:N0} đ</span></p>
      <p><b>Dịch vụ:</b></p>
      <ul>{combos}</ul>
      <p style=""margin-top:20px"">File PDF vé điện tử và mã QR được đính kèm trong email này. Vui lòng xuất trình QR theo từng vé tại rạp khi xem phim.</p>
    </div>
  </div>
</div>";    }
}

