using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Payments;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private static readonly TimeSpan SeatHoldDuration = TimeSpan.FromMinutes(5);
    private readonly AppDbContext _db;
    private readonly VnPaySignatureValidator _validator;
    private readonly ILoyaltyPointService _loyaltyPointService;
    private readonly IAdminVoucherService _voucherService;

    public PaymentService(AppDbContext db, VnPaySignatureValidator validator, ILoyaltyPointService loyaltyPointService, IAdminVoucherService voucherService)
    {
        _db = db;
        _validator = validator;
        _loyaltyPointService = loyaltyPointService;
        _voucherService = voucherService;
    }

    public async Task<PaymentCallbackResult> HandleVnPayReturnAsync(IDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        var process = await ProcessVnPayCallbackAsync(query, updateDatabase: true, cancellationToken);
        return new PaymentCallbackResult
        {
            Success = process.Success,
            SignatureValid = process.SignatureValid,
            TransactionRef = process.TransactionRef,
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

        var signatureValid = _validator.Validate(query);
        if (!signatureValid)
            return CallbackProcessResult.Fail("97", "Invalid signature", txnRef, responseCode, false);

        var payment = await _db.Payments.FirstOrDefaultAsync(t => t.GatewayTxnRef == txnRef, cancellationToken);
        if (payment == null)
            return CallbackProcessResult.Fail("01", "Order not found", txnRef, responseCode, true);

        var vnpAmountRaw = Get(query, "vnp_Amount");
        if (!long.TryParse(vnpAmountRaw, out var vnpAmountX100))
            return CallbackProcessResult.Fail("04", "invalid amount", txnRef, responseCode, true);

        var vnpAmount = vnpAmountX100 / 100m;
        if (payment.SoTien != vnpAmount)
            return CallbackProcessResult.Fail("04", "invalid amount", txnRef, responseCode, true);

        if (await ExpirePaymentIfHoldTimedOutAsync(payment, cancellationToken))
        {
            return new CallbackProcessResult
            {
                Success = false,
                SignatureValid = true,
                TransactionRef = txnRef,
                ResponseCode = responseCode,
                RspCode = "98",
                IpnMessage = "Order expired",
                Message = "Đơn hàng đã hết thời gian giữ ghế."
            };
        }

        if (!string.Equals(payment.TrangThai, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return new CallbackProcessResult
            {
                Success = PaymentStatuses.IsPaid(payment.TrangThai),
                SignatureValid = true,
                TransactionRef = txnRef,
                ResponseCode = responseCode,
                RspCode = "02",
                IpnMessage = "Order already confirmed",
                Message = "Giao dịch đã được xác nhận trước đó."
            };
        }

        var paid = responseCode == "00" && transactionStatus == "00";

        if (updateDatabase)
        {
            payment.GatewayTransNo = transactionNo;
            payment.BankCode = bankCode;
            payment.CardType = cardType;
            payment.VnpResponseCode = responseCode;
            payment.VnpTransactionStatus = transactionStatus;
            payment.SecureHash = secureHash;

            if (DateTime.TryParseExact(payDateRaw, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var payDate))
                payment.PayDate = payDate;

            var tickets = await _db.Tickets.Where(v => v.GatewayTxnRef == txnRef).ToListAsync(cancellationToken);
            if (paid)
            {
                payment.TrangThai = PaymentStatuses.Paid;
                foreach (var ticket in tickets)
                    ticket.TrangThai = PaymentStatuses.Paid;
            }
            else
            {
                payment.TrangThai = PaymentStatuses.Failed;
                foreach (var ticket in tickets)
                    ticket.TrangThai = PaymentStatuses.Failed;
            }

            if (paid)
                await EnsureInvoiceAsync(payment, tickets, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            if (paid)
                await TryEarnLoyaltyPointsAsync(txnRef, cancellationToken);
            else
            {
                await TryRefundLoyaltyPointsAsync(txnRef, cancellationToken);
                await _voucherService.ReopenVoucherForFailedPaymentAsync(txnRef, cancellationToken);
            }
        }

        return new CallbackProcessResult
        {
            Success = paid,
            SignatureValid = true,
            TransactionRef = txnRef,
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
        public string ResponseCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RspCode { get; set; } = "99";
        public string IpnMessage { get; set; } = "Unknown error";

        public static CallbackProcessResult Fail(string rspCode, string message, string txnRef = "", string responseCode = "", bool signatureValid = true)
            => new()
            {
                Success = false,
                SignatureValid = signatureValid,
                TransactionRef = txnRef,
                ResponseCode = responseCode,
                Message = message,
                RspCode = rspCode,
                IpnMessage = message
            };
    }
}













