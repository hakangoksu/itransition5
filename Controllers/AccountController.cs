using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models;
using UserManagement.Services;
using UserManagement.ViewModels;

namespace UserManagement.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked.");
            return View(model);
        }

        if (!user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Please verify your email address before logging in. Check your inbox.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            user.LastLoginTime = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Name,
            Email = model.Email,
            RegistrationTime = DateTime.UtcNow,
            Status = UserStatus.Unverified
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                "VerifyEmail",
                "Account",
                new { userId = user.Id, token },
                protocol: Request.Scheme)!;

            await _emailService.SendVerificationEmailAsync(user.Email!, user.UserName!, callbackUrl);

            TempData["SuccessMessage"] = "Registration successful! A verification email has been sent.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return RedirectToAction(nameof(Login));

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            if (!user.IsBlocked)
            {
                user.Status = UserStatus.Active;
                await _userManager.UpdateAsync(user);
            }

            TempData["SuccessMessage"] = "Email verified successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Email verification failed.";
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Admin");
    }
}
