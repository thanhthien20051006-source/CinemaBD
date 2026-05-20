using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CinemaBD.Web.Configurations;
using Microsoft.Extensions.Options;

namespace CinemaBD.Web.Infrastructure.Payments;

// ─── Models ──────────────────────────────────────────────────────────────────
public class MomoOrderInfo
{
    public string FullName  { get; set; } = "Khách hàng";
    public string OrderId   { get; set; } = string.Empty;
    public string OrderInfo { get; set; } = string.Empty;
    public long   Amount    { get; set; }
}

public class MomoCreatePaymentResponse
{
    [JsonPropertyName("requestId")]       public string RequestId       { get; set; } = "";
    [JsonPropertyName("errorCode")]       public int    ErrorCode       { get; set; }
    [JsonPropertyName("resultCode")]      public int?   ResultCode      { get; set; }
    [JsonPropertyName("orderId")]         public string OrderId         { get; set; } = "";
    [JsonPropertyName("message")]         public string Message         { get; set; } = "";
    [JsonPropertyName("localMessage")]    public string LocalMessage    { get; set; } = "";
    [JsonPropertyName("requestType")]     public string RequestType     { get; set; } = "";
    [JsonPropertyName("payUrl")]          public string PayUrl          { get; set; } = "";
    [JsonPropertyName("signature")]       public string Signature       { get; set; } = "";
    [JsonPropertyName("qrCodeUrl")]       public string QrCodeUrl       { get; set; } = "";
    [JsonPropertyName("deeplink")]        public string Deeplink        { get; set; } = "";
    [JsonPropertyName("deeplinkWebInApp")] public string DeeplinkWebInApp { get; set; } = "";
}

public class MomoExecuteResponse
{
    public string OrderId   { get; set; } = "";
    public string Amount    { get; set; } = "";
    public string FullName  { get; set; } = "";
    public string OrderInfo { get; set; } = "";
}

// ─── Interface ───────────────────────────────────────────────────────────────
public interface IMomoService
{
    Task<MomoCreatePaymentResponse> CreatePaymentAsync(MomoOrderInfo model);
    MomoExecuteResponse             PaymentExecute(IQueryCollection collection);
}

// ─── Implementation ──────────────────────────────────────────────────────────
public class MomoService : IMomoService
{
    private readonly MomoOptionModel _options;
    private readonly HttpClient      _http;

    public MomoService(IOptions<MomoOptionModel> options, HttpClient http)
    {
        _options = options.Value;
        _http    = http;
    }

    public async Task<MomoCreatePaymentResponse> CreatePaymentAsync(MomoOrderInfo model)
    {
        // Tao OrderId = timestamp neu chua co
        if (string.IsNullOrWhiteSpace(model.OrderId))
            model.OrderId = DateTime.UtcNow.Ticks.ToString();

        model.OrderInfo = $"Khách hàng: {model.FullName}. Nội dung: {model.OrderInfo}";

        // B1: rawData dung thu tu CHINH XAC theo tai lieu MoMo
        var rawData =
            $"partnerCode={_options.PartnerCode}" +
            $"&accessKey={_options.AccessKey}" +
            $"&requestId={model.OrderId}" +
            $"&amount={model.Amount}" +
            $"&orderId={model.OrderId}" +
            $"&orderInfo={model.OrderInfo}" +
            $"&returnUrl={_options.ReturnUrl}" +
            $"&notifyUrl={_options.NotifyUrl}" +
            $"&extraData=";

        // B2: HMAC-SHA256 (MoMo dung SHA256, khac VNPAY dung SHA512)
        var signature = ComputeHmacSha256(rawData, _options.SecretKey);

        // B3: Build request body
        var requestData = new
        {
            accessKey   = _options.AccessKey,
            partnerCode = _options.PartnerCode,
            requestType = _options.RequestType,
            notifyUrl   = _options.NotifyUrl,
            returnUrl   = _options.ReturnUrl,
            orderId     = model.OrderId,
            amount      = model.Amount.ToString(),
            orderInfo   = model.OrderInfo,
            requestId   = model.OrderId,
            extraData   = "",
            signature
        };

        try
        {
            var json     = JsonSerializer.Serialize(requestData);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_options.MomoApiUrl, content);
            var raw      = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MomoCreatePaymentResponse>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null)
            {
                result.ErrorCode = result.ResultCode ?? result.ErrorCode;
                if (result.ErrorCode == 0 && !string.IsNullOrWhiteSpace(result.PayUrl))
                    return result;
            }
        }
        catch { /* sandbox khong kha dung -> fallback demo */ }

        // ─── DEMO FALLBACK: mo phong thanh toan ─────────────────────────────
        return new MomoCreatePaymentResponse
        {
            ErrorCode = 0,
            OrderId   = model.OrderId,
            Message   = "Demo",
            PayUrl    = $"/booking/momo-demo?orderId={Uri.EscapeDataString(model.OrderId)}" +
                        $"&amount={model.Amount}" +
                        $"&orderInfo={Uri.EscapeDataString(model.OrderInfo)}"
        };
    }

    public MomoExecuteResponse PaymentExecute(IQueryCollection collection)
    {
        return new MomoExecuteResponse
        {
            Amount    = collection["amount"].FirstOrDefault()    ?? "",
            OrderId   = collection["orderId"].FirstOrDefault()   ?? "",
            OrderInfo = collection["orderInfo"].FirstOrDefault() ?? "",
            FullName  = collection["fullName"].FirstOrDefault()  ?? "Khách hàng"
        };
    }

    private static string ComputeHmacSha256(string message, string secretKey)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(secretKey);
        var msgBytes  = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

