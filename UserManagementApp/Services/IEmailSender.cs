namespace UserManagementApp.Services;

public interface IEmailSender
{
    // important: this just enqueues the e-mail; actual sending happens in the background,
    // so the registration request returns to the user immediately (asynchronous sending requirement)
    void EnqueueEmail(string toEmail, string subject, string htmlBody);
}