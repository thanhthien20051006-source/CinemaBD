namespace CinemaBD.Infrastructure.Payments;

internal static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
    public const string Refunded = "Refunded";
    public const string CancelRequested = "CancelRequested";

    public static bool IsPaid(string? status)
        => string.Equals(status, Paid, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Thành công", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);
}
