namespace UserManagementApp.Services;

// note: strongly-typed binding for the "EmailSettings" section in appsettings.json
public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}