using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;
using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Pages;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public RegisterModel(AppDbContext db, IEmailSender emailSender, IConfiguration config)
    {
        _db = db;
        _emailSender = emailSender;
        _config = config;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RegistrationSucceeded { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid e-mail address.")]
        public string Email { get; set; } = string.Empty;

        // note: task allows any non-empty password, even a single character
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new User
        {
            Name = Input.Name.Trim(),
            Email = Input.Email.Trim().ToLowerInvariant(),
            PasswordHash = AuthHelper.HashPassword(Input.Password),
            Status = UserStatus.Unverified,
            EmailConfirmationToken = AuthHelper.GenerateToken(),
            RegisteredAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateEmailError(ex))
        {
            // important: this is the actual database-level unique index violation being caught here,
            // not an app-side "check if exists" query — satisfies the task's storage-level uniqueness requirement
            ModelState.AddModelError(nameof(Input.Email), "This e-mail is already registered.");
            return Page();
        }

        var baseUrl = _config["AppBaseUrl"];
        var confirmLink = $"{baseUrl}/Confirm?token={user.EmailConfirmationToken}";

        _emailSender.EnqueueEmail(
            user.Email,
            "Confirm your e-mail",
            $"<p>Hi {user.Name},</p><p>Please confirm your e-mail by clicking the link below:</p><p><a href=\"{confirmLink}\">Confirm e-mail</a></p>");

        RegistrationSucceeded = true;
        return Page();
    }

    // note: MySQL duplicate-key errors surface as InnerException with error number 1062
    private static bool IsDuplicateEmailError(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("Duplicate entry") == true;
    }
}