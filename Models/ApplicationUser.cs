using Microsoft.AspNetCore.Identity;

namespace UserManagement.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginTime { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Unverified;
    public bool IsBlocked { get; set; } = false;
}

public enum UserStatus
{
    Unverified = 0,
    Active = 1,
    Blocked = 2
}