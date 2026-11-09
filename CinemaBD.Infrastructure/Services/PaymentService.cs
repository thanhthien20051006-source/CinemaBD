using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Payments;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private static readonly TimeSpan SeatHoldDuration = TimeSpan.FromMinutes(5);
    private readonly AppDbContext _db;
    private readonly VnPaySignatureValidator _validator;
    private readonly ILoyaltyPointService _loyaltyPointService;
    private readonly IAdminVoucherService _voucherService;
    private readonly VnPayUrlBuilder _vnPayUrlBuilder;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        AppDbContext db,
        VnPaySignatureValidator validator,
        ILoyaltyPointService loyaltyPointService,
        IAdminVoucherService voucherService,
        VnPayUrlBuilder vnPayUrlBuilder,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _validator = validator;
        _loyaltyPointService = loyaltyPointService;
        _voucherService = voucherService;
        _vnPayUrlBuilder = vnPayUrlBuilder;
        _logger = logger;
    }

    public async Task<VnPayCreatePaymentResult> CreateVnPayPaymentUrlAsync(
        string maHD,
        string currentUserId,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(maHD))
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Mã hóa đơn không hợp lệ."
            };
        }

        // Thống nhất một khóa tra cứu:
        // - Trong Database (bảng HoaDons, Payments, VE): GatewayTxnRef
        // - Trong tham số VNPay và redirect/deep link: vnp_TxnRef / txnRef
        var invoice = await _db.Invoices.FirstOrDefaultAsync(
            h => h.MaHD == maHD || h.GatewayTxnRef == maHD,
            cancellationToken);

        if (invoice == null)
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Không tìm thấy hóa đơn."
            };
        }

        // 1. Kiểm tra hóa đơn thuộc khách đang đăng nhập (trừ admin)
        if (!isAdmin && (string.IsNullOrWhiteSpace(currentUserId) || !string.Equals(invoice.MaKH, currentUserId, StringComparison.OrdinalIgnoreCase)))
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Hóa đơn không thuộc về người dùng hiện tại."
            };
        }

        // 2. Kiểm tra chưa Paid
        if (PaymentStatuses.IsPaid(invoice.TrangThai))
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Hóa đơn đã được thanh toán trước đó."
            };
        }

        // 3. Kiểm tra còn trong hạn giữ ghế (quy ước 5 phút)
        var holdCutoff = DateTime.Now.Subtract(SeatHoldDuration);
        var tickets = await _db.Tickets
            .Where(t => t.GatewayTxnRef == invoice.GatewayTxnRef)
            .ToListAsync(cancellationToken);

        bool isExpired = false;
        if (tickets.Count > 0)
        {
            if (tickets.Any(t => t.NgayDat <= holdCutoff || string.Equals(t.TrangThai, PaymentStatuses.Expired, StringComparison.OrdinalIgnoreCase)))
            {
                isExpired = true;
            }
        }
        else if (invoice.NgayThanhToan <= holdCutoff && string.Equals(invoice.TrangThai, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            isExpired = true;
        }

        if (isExpired)
        {
            invoice.TrangThai = PaymentStatuses.Expired;
            foreach (var t in tickets.Where(v => v.TrangThai == PaymentStatuses.Pending))
            {
                t.TrangThai = PaymentStatuses.Expired;
            }
            await _db.SaveChangesAsync(cancellationToken);

            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Đơn hàng đã hết thời gian giữ ghế."
            };
        }

        // 4. Số tiền lấy từ DB, không lấy từ client; kiểm tra decimal nhân 100 không lệch
        if (invoice.TongTien <= 0)
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Số tiền hóa đơn không hợp lệ."
            };
        }

        decimal amountX100 = invoice.TongTien * 100m;
        if (amountX100 != Math.Truncate(amountX100) || amountX100 > long.MaxValue)
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Số tiền hóa đơn không hợp lệ khi quy đổi sang định dạng VNPay."
            };
        }

        long vnpAmountLong = (long)amountX100;
        if ((decimal)vnpAmountLong / 100m != invoice.TongTien)
        {
            return new VnPayCreatePaymentResult
            {
                Success = false,
                Message = "Sai lệch khi quy đổi số tiền thanh toán."
            };
        }

        // 5. Thống nhất khóa tra cứu GatewayTxnRef
        if (string.IsNullOrWhiteSpace(invoice.GatewayTxnRef))
        {
            invoice.GatewayTxnRef = "TXN" + DateTime.UtcNow.ToString("yyMMddHHmmssfff");
            await _db.SaveChangesAsync(cancellationToken);
        }

        var paymentUrl = _vnPayUrlBuilder.Build(
            invoice.TongTien,
            invoice.GatewayTxnRef,
            string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress);

        return new VnPayCreatePaymentResult
        {
            Success = true,
            Message = "Tạo URL thanh toán VNPay thành công.",
            PaymentUrl = paymentUrl,
            TxnRef = invoice.GatewayTxnRef,
            MaHD = invoice.MaHD
        };
    }

    public async Task<PaymentCallbackResult> HandleVnPayReturnAsync(IDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        var process = await ProcessVnPayCallbackAsync(query, updateDatabase: true, cancellationToken);
        return new PaymentCallbackResult
        {
            Success = process.Success,
            SignatureValid = process.SignatureValid,
            TransactionRef = process.TransactionRef,
            InvoiceId = process.InvoiceId,
            ResponseCode = process.ResponseCode,
            Message = process.Message
        };
    }

    public async Task<VnPayIpnResult> HandleVnPayIpnAsync(IDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        var process = await ProcessVnPayCallbackAsync(query, updateDatabase: true, cancellationToken);
        return new VnPayIpnResult
        {
            RspCode = process.RspCode,
            Message = process.IpnMessage
        };
    }

    public async Task<PaymentCallbackResult> ConfirmDemoPaymentAsync(string txnRef, string gateway, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(txnRef))
        {
            return new PaymentCallbackResult
            {
                Success = false,
                SignatureValid = true,
                TransactionRef = txnRef,
                ResponseCode = "99",
                Message = "Thiếu mã giao dịch."
            };
        }

        var payment = await _db.Payments.FirstOrDefaultAsync(t => t.GatewayTxnRef == txnRef, cancellationToken);
        if (payment == null)
        {
            return new PaymentCallbackResult
            {
                Success = false,
                SignatureValid = true,
                TransactionRef = txnRef,
                ResponseCode = "01",
                Message = "Không tìm thấy đơn hàng."
            };
        }

        if (await ExpirePaymentIfHoldTimedOutAsync(payment, cancellationToken))
        {
            return new PaymentCallbackResult
            {
                Success = false,
                SignatureValid = true,
                TransactionRef = txnRef,
                ResponseCode = "98",
                Message = "Đơn hàng đã hết thời gian giữ ghế."
            };
        }

        if (!string.Equals(payment.TrangThai, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentCallbackResult
            {
                Success = PaymentStatuses.IsPaid(payment.TrangThai),
                SignatureValid = true,
                TransactionRef = txnRef,
                ResponseCode = string.Equals(payment.TrangThai, PaymentStatuses.Expired, StringComparison.OrdinalIgnoreCase) ? "98" : "02",
                Message = string.Equals(payment.TrangThai, PaymentStatuses.Expired, StringComparison.OrdinalIgnoreCase)
                    ? "Đơn hàng đã hết thời gian giữ ghế."
                    : "Giao dịch đã được xác nhận trước đó."
            };
        }

        payment.TrangThai = PaymentStatuses.Paid;
        payment.PaymentGateway = string.IsNullOrWhiteSpace(gateway) ? payment.PaymentGateway : gateway;
        payment.PayDate = DateTime.Now;
        payment.VnpResponseCode = "00";
        payment.VnpTransactionStatus = "00";
        payment.GatewayTransNo = txnRef;

        var tickets = await _db.Tickets.Where(v => v.GatewayTxnRef == txnRef).ToListAsync(cancellationToken);
        foreach (var ticket in tickets)
            ticket.TrangThai = PaymentStatuses.Paid;

        await EnsureInvoiceAsync(payment, tickets, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await TryEarnLoyaltyPointsAsync(txnRef, cancellationToken);

        return new PaymentCallbackResult
        {
            Success = true,
            SignatureValid = true,
            TransactionRef = txnRef,
            ResponseCode = "00",
            Message = "Thanh toán thành công."
        };
    }

    private async Task<CallbackProcessResult> ProcessVnPayCallbackAsync(IDictionary<string, string> query, bool updateDatabase, CancellationToken cancellationToken)
    {
        if (query.Count == 0)
            return CallbackProcessResult.Fail("99", "Input data required", signatureValid: false);

        var txnRef = Get(query, "vnp_TxnRef");
        var responseCode = Get(query, "vnp_ResponseCode");
        var transactionStatus = Get(query, "vnp_TransactionStatus");
        var transactionNo = Get(query, "vnp_TransactionNo");
        var bankCode = Get(query, "vnp_BankCode");
        var cardType = Get(query, "vnp_CardType");
        var secureHash = Get(query, "vnp_SecureHash");
        var payDateRaw = Get(query, "vnp_PayDate");
        var vnpAmountRaw = Get(query, "vnp_Amount");

        // 1. Xác minh chữ ký TRƯỚC (NIST CSF 1.1 Protect & Detect)
        var signatureValid = _validator.Validate(query);
        if (!signatureValid)
        {
            // Ghi log [SECURITY_AUDIT] KHÔNG chứa HashSecret hoặc TmnCode
            _logger.LogWarning("[SECURITY_AUDIT] Chữ ký VNPay không hợp lệ. TxnRef: {TxnRef}, SecureHash: {SecureHash}, ResponseCode: {ResponseCode}",
                txnRef, secureHash, responseCode);
            // Không sửa DB
            return CallbackProcessResult.Fail("97", "Chữ ký không hợp lệ.", txnRef, responseCode, signatureValid: false);
        }

        // 2. Tra cứu hóa đơn theo GatewayTxnRef thống nhất
        var invoice = await _db.Invoices.FirstOrDefaultAsync(h => h.GatewayTxnRef == txnRef || h.MaHD == txnRef, cancellationToken);
        var payment = await _db.Payments.FirstOrDefaultAsync(t => t.GatewayTxnRef == txnRef, cancellationToken);

        if (invoice == null && payment == null)
            return CallbackProcessResult.Fail("01", "Không tìm thấy hóa đơn/đơn hàng.", txnRef, responseCode, signatureValid: true);

        var invoiceId = invoice?.MaHD ?? string.Empty;
        var totalAmount = invoice?.TongTien ?? payment?.SoTien ?? 0m;

        // 3. Kiểm tra vnp_Amount / 100 khớp hóa đơn
        if (!long.TryParse(vnpAmountRaw, out var vnpAmountX100))
        {
            _logger.LogWarning("[SECURITY_AUDIT] Định dạng vnp_Amount không hợp lệ: {RawAmount}, TxnRef: {TxnRef}", vnpAmountRaw, txnRef);
            return CallbackProcessResult.Fail("04", "Số tiền không hợp lệ.", txnRef, responseCode, signatureValid: true, invoiceId: invoiceId);
        }

        var vnpAmount = vnpAmountX100 / 100m;
        if (totalAmount != vnpAmount)
        {
            _logger.LogWarning("[SECURITY_AUDIT] Số tiền thanh toán không khớp với hóa đơn. DB: {Expected}, VNPay: {Actual}, TxnRef: {TxnRef}", totalAmount, vnpAmount, txnRef);
            return CallbackProcessResult.Fail("04", "Số tiền thanh toán không khớp với hóa đơn.", txnRef, responseCode, signatureValid: true, invoiceId: invoiceId);
        }

        var currentStatus = invoice?.TrangThai ?? payment?.TrangThai ?? string.Empty;

        // 4. Nếu đã Paid: "Đã Paid thì không ghi lại."
        if (PaymentStatuses.IsPaid(currentStatus))
        {
            return new CallbackProcessResult
            {
                Success = true,
                SignatureValid = true,
                TransactionRef = txnRef,
                InvoiceId = invoiceId,
                ResponseCode = responseCode,
                RspCode = "00",
                IpnMessage = "Order already confirmed",
                Message = "Giao dịch đã được xác nhận trước đó."
            };
        }

        // 5. Kiểm tra thời gian giữ ghế
        if (payment != null && await ExpirePaymentIfHoldTimedOutAsync(payment, cancellationToken))
        {
            if (invoice != null && string.Equals(invoice.TrangThai, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                invoice.TrangThai = PaymentStatuses.Expired;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new CallbackProcessResult
            {
                Success = false,
                SignatureValid = true,
                TransactionRef = txnRef,
                InvoiceId = invoiceId,
                ResponseCode = responseCode,
                RspCode = "98",
                IpnMessage = "Order expired",
                Message = "Đơn hàng đã hết thời gian giữ ghế."
            };
        }

        var paid = responseCode == "00" && (string.IsNullOrWhiteSpace(transactionStatus) || transactionStatus == "00");

        if (updateDatabase)
        {
            if (paid)
            {
                // Cập nhật Paid bằng UPDATE có điều kiện WHERE TrangThai = 'Pending' (kiểm tra số dòng ảnh hưởng)
                int affectedInvoiceRows = 0;
                if (invoice != null)
                {
                    affectedInvoiceRows = await _db.Invoices
                        .Where(h => h.MaHD == invoice.MaHD && h.TrangThai == PaymentStatuses.Pending)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(h => h.TrangThai, PaymentStatuses.Paid)
                            .SetProperty(h => h.NgayThanhToan, DateTime.Now)
                            .SetProperty(h => h.GhiChu, $"Thanh toán VNPay {transactionNo}"),
                            cancellationToken);
                }

                if (payment != null)
                {
                    await _db.Payments
                        .Where(p => p.GatewayTxnRef == txnRef && p.TrangThai == PaymentStatuses.Pending)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.TrangThai, PaymentStatuses.Paid)
                            .SetProperty(p => p.PayDate, DateTime.Now)
                            .SetProperty(p => p.GatewayTransNo, transactionNo)
                            .SetProperty(p => p.BankCode, bankCode)
                            .SetProperty(p => p.CardType, cardType)
                            .SetProperty(p => p.VnpResponseCode, responseCode)
                            .SetProperty(p => p.VnpTransactionStatus, transactionStatus ?? "00")
                            .SetProperty(p => p.SecureHash, secureHash),
                            cancellationToken);
                }

                await _db.Tickets
                    .Where(v => v.GatewayTxnRef == txnRef && v.TrangThai == PaymentStatuses.Pending)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.TrangThai, PaymentStatuses.Paid), cancellationToken);

                if (invoice == null && payment != null)
                {
                    var tickets = await _db.Tickets.Where(v => v.GatewayTxnRef == txnRef).ToListAsync(cancellationToken);
                    await EnsureInvoiceAsync(payment, tickets, cancellationToken);
                    invoice = await _db.Invoices.FirstOrDefaultAsync(h => h.GatewayTxnRef == txnRef, cancellationToken);
                    invoiceId = invoice?.MaHD ?? string.Empty;
                }

                await TryEarnLoyaltyPointsAsync(txnRef, cancellationToken);
            }
            else
            {
                if (invoice != null)
                {
                    await _db.Invoices
                        .Where(h => h.MaHD == invoice.MaHD && h.TrangThai == PaymentStatuses.Pending)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(h => h.TrangThai, PaymentStatuses.Failed)
                            .SetProperty(h => h.GhiChu, $"VNPay thất bại ({responseCode})"),
                            cancellationToken);
                }

                if (payment != null)
                {
                    await _db.Payments
                        .Where(p => p.GatewayTxnRef == txnRef && p.TrangThai == PaymentStatuses.Pending)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.TrangThai, PaymentStatuses.Failed)
                            .SetProperty(p => p.VnpResponseCode, responseCode)
                            .SetProperty(p => p.VnpTransactionStatus, transactionStatus)
                            .SetProperty(p => p.SecureHash, secureHash),
                            cancellationToken);
                }

                await _db.Tickets
                    .Where(v => v.GatewayTxnRef == txnRef && v.TrangThai == PaymentStatuses.Pending)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.TrangThai, PaymentStatuses.Failed), cancellationToken);

                await TryRefundLoyaltyPointsAsync(txnRef, cancellationToken);
                await _voucherService.ReopenVoucherForFailedPaymentAsync(txnRef, cancellationToken);
            }
        }

        return new CallbackProcessResult
        {
            Success = paid,
            SignatureValid = true,
            TransactionRef = txnRef,
            InvoiceId = invoiceId,
            ResponseCode = responseCode,
            RspCode = "00",
            IpnMessage = "Confirm Success",
            Message = paid ? "Thanh toán thành công." : $"Thanh toán thất bại. Mã lỗi: {responseCode}"
        };
    }

    private async Task EnsureInvoiceAsync(LegacyPayment payment, List<LegacyTicket> tickets, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payment.GatewayTxnRef) || tickets.Count == 0)
            return;

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.GatewayTxnRef == payment.GatewayTxnRef, cancellationToken);
        var firstTicket = tickets.First();

        if (invoice == null)
        {
            invoice = new LegacyInvoice
            {
                MaHD = "HD" + DateTime.Now.Ticks,
                GatewayTxnRef = payment.GatewayTxnRef
            };
            _db.Invoices.Add(invoice);
        }

        invoice.MaKH = firstTicket.MaKH ?? invoice.MaKH ?? string.Empty;
        invoice.TongTien = payment.SoTien;
        invoice.NgayThanhToan = payment.PayDate ?? DateTime.Now;
        invoice.TrangThai = PaymentStatuses.Paid;
        invoice.GhiChu = $"Thanh toán {payment.PaymentGateway ?? payment.HinhThuc ?? "Online"}";

        await _db.SaveChangesAsync(cancellationToken);

        var existingTicketLineIds = await _db.InvoiceLineItems
            .Where(x => x.MaHD == invoice.MaHD && x.MaVe != null)
            .Select(x => x.MaVe!)
            .ToListAsync(cancellationToken);

        foreach (var ticket in tickets)
        {
            if (existingTicketLineIds.Contains(ticket.MaVe))
                continue;

            _db.InvoiceLineItems.Add(new LegacyInvoiceLineItem
            {
                MaHD = invoice.MaHD,
                LoaiDong = "Ve",
                MaVe = ticket.MaVe,
                TenDong = $"Ghế {ticket.MaGhe}",
                SoLuong = 1,
                DonGia = ticket.GiaVe
            });
        }

        var combos = await _db.BookedCombos.AsNoTracking()
            .Where(x => x.GatewayTxnRef == payment.GatewayTxnRef)
            .ToListAsync(cancellationToken);
        var comboIds = combos.Select(x => x.MaCombo).Distinct().ToList();
        var comboNames = await _db.Combos.AsNoTracking()
            .Where(x => comboIds.Contains(x.MaCombo))
            .ToDictionaryAsync(x => x.MaCombo, x => x.TenCombo, cancellationToken);

        var existingComboLineIds = await _db.InvoiceLineItems
            .Where(x => x.MaHD == invoice.MaHD && x.MaCombo != null)
            .Select(x => x.MaCombo!)
            .ToListAsync(cancellationToken);

        foreach (var combo in combos)
        {
            if (existingComboLineIds.Contains(combo.MaCombo))
                continue;

            _db.InvoiceLineItems.Add(new LegacyInvoiceLineItem
            {
                MaHD = invoice.MaHD,
                LoaiDong = "Combo",
                MaCombo = combo.MaCombo,
                TenDong = comboNames.GetValueOrDefault(combo.MaCombo, combo.MaCombo),
                SoLuong = combo.SoLuong,
                DonGia = combo.SoLuong <= 0 ? combo.Gia : combo.Gia / combo.SoLuong
            });
        }
    }

    private async Task<bool> ExpirePaymentIfHoldTimedOutAsync(LegacyPayment payment, CancellationToken cancellationToken)
    {
        if (!string.Equals(payment.TrangThai, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return false;

        var expiredBefore = DateTime.Now.Subtract(SeatHoldDuration);
        var tickets = await _db.Tickets
            .Where(v => v.GatewayTxnRef == payment.GatewayTxnRef)
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0 || tickets.Any(v => v.NgayDat > expiredBefore))
            return false;

        payment.TrangThai = PaymentStatuses.Expired;
        foreach (var ticket in tickets.Where(v => v.TrangThai == PaymentStatuses.Pending))
            ticket.TrangThai = PaymentStatuses.Expired;

        await _db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(payment.GatewayTxnRef))
        {
            await TryRefundLoyaltyPointsAsync(payment.GatewayTxnRef, cancellationToken);
            await _voucherService.ReopenVoucherForFailedPaymentAsync(payment.GatewayTxnRef, cancellationToken);
        }
        return true;
    }
    private async Task TryEarnLoyaltyPointsAsync(string txnRef, CancellationToken cancellationToken)
    {
        try
        {
            await _loyaltyPointService.EarnFromPaymentAsync(txnRef, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("TICHDIEM", StringComparison.OrdinalIgnoreCase))
        {
            // Database hiện tại chưa có bảng tích điểm; không được làm hỏng luồng thanh toán thành công.
        }
    }

    private async Task TryRefundLoyaltyPointsAsync(string txnRef, CancellationToken cancellationToken)
    {
        try
        {
            await _loyaltyPointService.RefundRedeemedPointsAsync(txnRef, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("TICHDIEM", StringComparison.OrdinalIgnoreCase))
        {
            // Database hiện tại chưa có bảng tích điểm; bỏ qua rollback điểm.
        }
    }

    private static string Get(IDictionary<string, string> query, string key)
        => query.TryGetValue(key, out var value) ? value : string.Empty;

    private sealed class CallbackProcessResult
    {
        public bool Success { get; set; }
        public bool SignatureValid { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RspCode { get; set; } = "99";
        public string IpnMessage { get; set; } = "Unknown error";

        public static CallbackProcessResult Fail(string rspCode, string message, string txnRef = "", string responseCode = "", bool signatureValid = true, string invoiceId = "")
            => new()
            {
                Success = false,
                SignatureValid = signatureValid,
                TransactionRef = txnRef,
                InvoiceId = invoiceId,
                ResponseCode = responseCode,
                Message = message,
                RspCode = rspCode,
                IpnMessage = message
            };
    }
}













