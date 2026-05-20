using Microsoft.Extensions.Configuration;

namespace CinemaBD.Infrastructure.Payments;

public class VnPaySignatureValidator
{
    private readonly IConfiguration _configuration;

    public VnPaySignatureValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool Validate(IDictionary<string, string> query)
    {
        var secret = _configuration["VnPay:HashSecret"] ?? string.Empty;
        return VnPayLibrary.ValidateSignature(query, secret);
    }
}
