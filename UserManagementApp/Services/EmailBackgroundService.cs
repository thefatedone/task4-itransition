using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace UserManagementApp.Services;

// note: represents a single queued e-mail job
public record EmailJob(string ToEmail, string Subject, string HtmlBody);

// important: this background service is the ONLY thing that actually sends e-mail.
// Pages just call EnqueueEmail and move on — the request never waits for the HTTP round-trip.
// note: uses Resend's HTTPS API instead of raw SMTP, since many free hosting platforms
// (e.g. Render's free tier) block outbound SMTP ports (25/465/587) but never block HTTPS (443).
public class EmailBackgroundService : BackgroundService, IEmailSender
{
    private readonly Channel<EmailJob> _queue = Channel.CreateUnbounded<EmailJob>();
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly HttpClient _httpClient;

    public EmailBackgroundService(IOptions<EmailSettings> settings, ILogger<EmailBackgroundService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = new Uri("https://api.resend.com/") };
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
        var payload = new
        {
            from = $"{_settings.SenderName} <{_settings.SenderEmail}>",
            to = new[] { job.ToEmail },
            subject = job.Subject,
            html = job.HtmlBody
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ResendApiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Resend API returned {(int)response.StatusCode}: {body}");
        }
    }
}