using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CinemaBD.Infrastructure.Payments;

public sealed class VnPayLibrary
{
    public const string Version = "2.1.0";
    public const string PayCommand = "pay";
    public const string CurrencyCode = "VND";

    private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
    private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

    public void AddRequestData(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            _requestData[key] = value.Trim();
    }

    public void AddResponseData(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            _responseData[key] = value.Trim();
    }

    public string GetResponseData(string key)
        => _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;

    public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
    {
        var data = new StringBuilder();

        foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)))
        {
            data.Append(WebUtility.UrlEncode(key));
            data.Append('=');
            data.Append(WebUtility.UrlEncode(value));
            data.Append('&');
        }

        var queryString = data.ToString();
        var signData = queryString;
        if (signData.Length > 0)
            signData = signData.Remove(signData.Length - 1, 1);

        var vnpSecureHash = HmacSHA512(vnpHashSecret.Trim(), signData);
        return $"{baseUrl.Trim()}?{queryString}vnp_SecureHash={vnpSecureHash}";
    }

    public bool ValidateSignature(string inputHash, string secretKey)
    {
        var rspRaw = GetResponseData();
        var myChecksum = HmacSHA512(secretKey.Trim(), rspRaw);
        return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private string GetResponseData()
    {
        _responseData.Remove("vnp_SecureHashType");
        _responseData.Remove("vnp_SecureHash");

        var data = new StringBuilder();
        foreach (var (key, value) in _responseData.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)))
        {
            data.Append(WebUtility.UrlEncode(key));
            data.Append('=');
            data.Append(WebUtility.UrlEncode(value));
            data.Append('&');
        }

        if (data.Length > 0)
            data.Remove(data.Length - 1, 1);

        return data.ToString();
    }

    public static string CreatePaymentUrl(string baseUrl, string hashSecret, IDictionary<string, string?> requestData)
    {
        var vnpay = new VnPayLibrary();
        foreach (var item in requestData)
            vnpay.AddRequestData(item.Key, item.Value);
        return vnpay.CreateRequestUrl(baseUrl, hashSecret);
    }

    public static bool ValidateSignature(IDictionary<string, string> responseData, string hashSecret)
    {
        if (!responseData.TryGetValue("vnp_SecureHash", out var inputHash) || string.IsNullOrWhiteSpace(inputHash))
            return false;

        var vnpay = new VnPayLibrary();
        foreach (var item in responseData.Where(x => x.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)))
            vnpay.AddResponseData(item.Key, item.Value);

        return vnpay.ValidateSignature(inputHash, hashSecret);
    }

    public static string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        foreach (var theByte in hashValue)
            hash.Append(theByte.ToString("x2"));
        return hash.ToString();
    }

    public static string GetIpAddress(HttpContext context)
    {
        try
        {
            var remoteIpAddress = context.Connection.RemoteIpAddress;
            if (remoteIpAddress == null)
                return "127.0.0.1";

            if (remoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                remoteIpAddress = Dns.GetHostEntry(remoteIpAddress)
                    .AddressList
                    .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            }

            return remoteIpAddress?.ToString() ?? "127.0.0.1";
        }
        catch (Exception ex)
        {
            return "Invalid IP:" + ex.Message;
        }
    }
}

public sealed class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}
