namespace CinemaBD.Web.Configurations;

public class MomoOptionModel
{
    public string MomoApiUrl   { get; set; } = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
    public string SecretKey    { get; set; } = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
    public string AccessKey    { get; set; } = "F8BBA842ECF85";
    public string ReturnUrl    { get; set; } = "https://localhost:7188/booking/momo-return";
    public string NotifyUrl    { get; set; } = "https://localhost:7188/booking/momo-notify";
    public string PartnerCode  { get; set; } = "MOMO";
    public string RequestType  { get; set; } = "captureMoMoWallet";
}
