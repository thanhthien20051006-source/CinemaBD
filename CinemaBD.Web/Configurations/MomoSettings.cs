namespace CinemaBD.Web.Configurations;

public class MomoOptionModel
{
    public string MomoApiUrl   { get; set; } = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
    public string SecretKey    { get; set; } = string.Empty;
    public string AccessKey    { get; set; } = string.Empty;
    public string ReturnUrl    { get; set; } = "http://localhost:7188/booking/momo-return";
    public string NotifyUrl    { get; set; } = "http://localhost:7188/booking/momo-notify";
    public string PartnerCode  { get; set; } = "MOMO";
    public string RequestType  { get; set; } = "captureMoMoWallet";
}
