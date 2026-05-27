namespace CinemaBD.Web.Configurations;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "CinemaBD";
    public string FromPassword { get; set; } = string.Empty;
    public string SMTPAccount { get; set; } = string.Empty;
    public string SMTPPassword { get; set; } = string.Empty;

    public string SenderEmail => string.IsNullOrWhiteSpace(SMTPAccount) ? FromEmail : SMTPAccount;
    public string SenderPassword => string.IsNullOrWhiteSpace(SMTPPassword) ? FromPassword : SMTPPassword;
}
