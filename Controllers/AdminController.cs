using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.IsBlocked)
            {
                await _signInManager.SignOutAsync();
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
        }

        await next();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.LastLoginTime ?? u.RegistrationTime)
            .ToListAsync();

        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUsers([FromForm] string[] userIds)
    {
        if (userIds == null || userIds.Length == 0)
            return Json(new { success = false, message = "No users selected" });

        var currentUserId = _userManager.GetUserId(User);
        var blockedCount = 0;

        foreach (var userId in userIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsBlocked = true;
                user.Status = UserStatus.Blocked;
                await _userManager.UpdateAsync(user);
                blockedCount++;

                if (userId == currentUserId)
                {
                    await _signInManager.SignOutAsync();
                }
            }
        }

        return Json(new { success = true, message = $"{blockedCount} user(s) blocked successfully", redirect = currentUserId != null && userIds.Contains(currentUserId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUsers([FromForm] string[] userIds)
    {
        if (userIds == null || userIds.Length == 0)
            return Json(new { success = false, message = "No users selected" });

        var unblockedCount = 0;

        foreach (var userId in userIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && user.IsBlocked)
            {
                user.IsBlocked = false;
                user.Status = user.EmailConfirmed ? UserStatus.Active : UserStatus.Unverified;
                await _userManager.UpdateAsync(user);
                unblockedCount++;
            }
        }

        return Json(new { success = true, message = $"{unblockedCount} user(s) unblocked successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUsers([FromForm] string[] userIds)
    {
        if (userIds == null || userIds.Length == 0)
            return Json(new { success = false, message = "No users selected" });

        var currentUserId = _userManager.GetUserId(User);
        var deletedCount = 0;

        foreach (var userId in userIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                deletedCount++;
            }
        }

        var selfDeleted = currentUserId != null && userIds.Contains(currentUserId);

        if (selfDeleted)
        {
            await _signInManager.SignOutAsync();
        }

        return Json(new { success = true, message = $"{deletedCount} user(s) deleted successfully", redirect = selfDeleted });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnverified()
    {
        var unverifiedUsers = await _userManager.Users
            .Where(u => u.Status == UserStatus.Unverified)
            .ToListAsync();

        var deletedCount = 0;
        foreach (var user in unverifiedUsers)
        {
            await _userManager.DeleteAsync(user);
            deletedCount++;
        }

        return Json(new { success = true, message = $"{deletedCount} unverified user(s) deleted successfully" });
    }
}