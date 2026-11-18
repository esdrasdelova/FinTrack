using FinTrack.Data;
using FinTrack.Models;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace FinTrack.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IDemoDataSeeder _seeder;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IDemoDataSeeder seeder,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _seeder = seeder;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            _logger.LogInformation("Usuário {Email} entrou no sistema.", user.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Conta bloqueada temporariamente. Tente novamente mais tarde.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", user.FullName));
        await _seeder.EnsureCategoriesAsync(user.Id);
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation("Novo usuário registrado: {Email}", user.Email);
        TempData["Success"] = "Bem-vindo ao FinTrack! Sua conta foi criada com sucesso.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Usuário encerrou a sessão.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            ViewBag.ResetLink = null;
            TempData["Info"] = "Se o e-mail estiver cadastrado, um link de recuperação será gerado.";
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token = encoded }, Request.Scheme);

        // Sem provedor de e-mail configurado, o link é exibido ao usuário (ambiente local/demonstração).
        ViewBag.ResetLink = link;
        ViewBag.ResetEmail = user.Email;
        _logger.LogInformation("Token de redefinição gerado para {Email}", user.Email);
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        return View(new ResetPasswordViewModel { Email = email ?? string.Empty, Token = token ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Solicitação inválida.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = "Senha redefinida. Faça login com a nova senha.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }
}
