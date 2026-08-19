using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace UserManagementApp.Services;

// note: represents a single queued e-mail job
public record EmailJob(string ToEmail, string Subject, string HtmlBody);

// important: this background service is the ONLY thing that actually talks to the SMTP server.
// Pages just call EnqueueEmail and move on — the request never waits for the SMTP round-trip.
public class EmailBackgroundService : BackgroundService, IEmailSender
{
    private readonly Channel<EmailJob> _queue = Channel.CreateUnbounded<EmailJob>();
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(IOptions<EmailSettings> settings, ILogger<EmailBackgroundService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public void EnqueueEmail(string toEmail, string subject, string htmlBody)
    {
        _queue.Writer.TryWrite(new EmailJob(toEmail, subject, htmlBody));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(job, stoppingToken);
                _logger.LogInformation("Email sent to {Email}", job.ToEmail);
            }
            catch (Exception ex)
            {
                // note: we log and move on — a failed e-mail should not crash the background worker
                _logger.LogError(ex, "Failed to send email to {Email}", job.ToEmail);
            }
        }
    }

    private async Task SendAsync(EmailJob job, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(job.ToEmail));
        message.Subject = job.Subject;
        message.Body = new TextPart("html") { Text = job.HtmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}