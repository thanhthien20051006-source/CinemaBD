using Microsoft.Extensions.Configuration;

namespace CinemaBD.Infrastructure.Payments;

public sealed class VnPayUrlBuilder
{
    private readonly IConfiguration _configuration;

    public VnPayUrlBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Build(decimal totalAmount, string txnRef, string ipAddress)
        => Build(totalAmount, txnRef, ipAddress, null);

    public string Build(decimal totalAmount, string txnRef, string ipAddress, string? returnUrlOverride)
    {
        var baseUrl = GetRequired("VnPay:BaseUrl");
        var tmnCode = GetRequired("VnPay:TmnCode");
        var returnUrl = string.IsNullOrWhiteSpace(returnUrlOverride) ? GetRequired("VnPay:ReturnUrl") : returnUrlOverride.Trim();
        var secret = GetRequired("VnPay:HashSecret");

        var now = GetVietnamNow();
        var requestData = new Dictionary<string, string?>
        {
            ["vnp_Version"] = VnPayLibrary.Version,
            ["vnp_Command"] = VnPayLibrary.PayCommand,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = ((long)(Math.Round(totalAmount, 0, MidpointRounding.AwayFromZero) * 100)).ToString(),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = VnPayLibrary.CurrencyCode,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan don hang {txnRef}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_ExpireDate"] = now.AddMinutes(30).ToString("yyyyMMddHHmmss")
        };

        return VnPayLibrary.CreatePaymentUrl(baseUrl, secret, requestData);
    }

    private string GetRequired(string key)
    {
        var value = _configuration[key]?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Thiếu cấu hình {key}.");
        return value;
    }

    private static DateTime GetVietnamNow()
    {
        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }
    }
}
