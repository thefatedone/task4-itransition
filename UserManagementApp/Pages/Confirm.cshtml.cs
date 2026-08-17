using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Pages;

public class ConfirmModel : PageModel
{
    private readonly AppDbContext _db;

    public ConfirmModel(AppDbContext db)
    {
        _db = db;
    }

    public string ResultMessage { get; set; } = string.Empty;
    public bool Success { get; set; }

    public async Task OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            ResultMessage = "Invalid confirmation link.";
            return;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

        if (user is null)
        {
            ResultMessage = "Invalid or expired confirmation link.";
            return;
        }

        // note: per task spec, a blocked user's status stays Blocked even if they click the link
        if (user.Status == UserStatus.Unverified)
        {
            user.Status = UserStatus.Active;
            user.EmailConfirmationToken = null;
            await _db.SaveChangesAsync();
        }

        Success = true;
        ResultMessage = "Your e-mail has been confirmed. You can now sign in.";
    }
}