using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models;

// note: this enum represents the three possible account states described in the task
public enum UserStatus
{
    Unverified,
    Active,
    Blocked
}

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // important: this field will be covered by a UNIQUE INDEX at the database level (not just an app-level check)
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Unverified;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // nota bene: nullable because a freshly registered user has never logged in yet
    public DateTime? LastLoginAt { get; set; }

    // note: token sent in the confirmation e-mail link; cleared once the account becomes Active
    public string? EmailConfirmationToken { get; set; }
}