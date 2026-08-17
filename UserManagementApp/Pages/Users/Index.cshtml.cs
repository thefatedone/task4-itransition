using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Pages.Users;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<User> UserList { get; set; } = new();
    public int CurrentUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        CurrentUserId = GetCurrentUserId();

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Name.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        UserList = await query
            .OrderByDescending(u => u.LastLoginAt.HasValue)
            .ThenByDescending(u => u.LastLoginAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostBlockAsync([FromBody] int[] ids)
    {
        var idList = ids.ToList();
        var users = await _db.Users.Where(u => idList.Any(id => id == u.Id)).ToListAsync();
        foreach (var u in users) u.Status = UserStatus.Blocked;
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true, message = $"{users.Count} user(s) blocked." });
    }

    public async Task<IActionResult> OnPostUnblockAsync([FromBody] int[] ids)
    {
        var idList = ids.ToList();
        var users = await _db.Users.Where(u => idList.Any(id => id == u.Id)).ToListAsync();
        foreach (var u in users) u.Status = UserStatus.Active;
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true, message = $"{users.Count} user(s) unblocked." });
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
    {
        var idList = ids.ToList();
        var users = await _db.Users.Where(u => idList.Any(id => id == u.Id)).ToListAsync();
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true, message = $"{users.Count} user(s) deleted." });
    }

    public async Task<IActionResult> OnPostDeleteUnverifiedAsync()
    {
        var users = await _db.Users.Where(u => u.Status == UserStatus.Unverified).ToListAsync();
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true, message = $"{users.Count} unverified user(s) deleted." });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}