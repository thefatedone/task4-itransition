namespace UserManagementApp.Services;

// note: strongly-typed binding for the "EmailSettings" section in appsettings.json
public class EmailSettings
{
    public string ResendApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}