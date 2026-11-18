using FinTrack.Extensions;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface ISettingsService
{
    Task<SettingsViewModel?> GetAsync(string userId);
    Task<(bool Success, string Message)> UpdateProfileAsync(string userId, ProfileFormViewModel model);
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, PasswordFormViewModel model);
}

public class SettingsService : ISettingsService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUnitOfWork _uow;

    public SettingsService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUnitOfWork uow)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _uow = uow;
    }

    public async Task<SettingsViewModel?> GetAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        return new SettingsViewModel
        {
            IsDemo = user.IsDemo,
            MemberSince = user.CreatedAt.ToString("dd 'de' MMMM 'de' yyyy", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),
            Profile = new ProfileFormViewModel
            {
                FullName = user.FullName, Email = user.Email ?? string.Empty, AvatarUrl = user.AvatarUrl,
                Currency = user.Currency, DateFormat = user.DateFormat, Theme = user.Theme,
                EmailNotifications = user.EmailNotifications, InAppNotifications = user.InAppNotifications,
                WeeklySummary = user.WeeklySummary, SecurityAlerts = user.SecurityAlerts
            },
            Counters = new Dictionary<string, int>
            {
                ["Transações"] = await _uow.Transactions.CountAsync(),
                ["Contas"] = await _uow.Accounts.CountAsync(),
                ["Cartões"] = await _uow.CreditCards.CountAsync(),
                ["Metas"] = await _uow.Goals.CountAsync(),
                ["Investimentos"] = await _uow.Investments.CountAsync()
            }
        };
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(string userId, ProfileFormViewModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, "Usuário não encontrado.");

        var existing = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (existing is not null && existing.Id != user.Id)
            return (false, "Este e-mail já está em uso por outra conta.");

        var oldName = user.FullName;
        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        user.Currency = model.Currency;
        user.DateFormat = model.DateFormat;
        user.Theme = model.Theme;
        user.EmailNotifications = model.EmailNotifications;
        user.InAppNotifications = model.InAppNotifications;
        user.WeeklySummary = model.WeeklySummary;
        user.SecurityAlerts = model.SecurityAlerts;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        if (oldName != user.FullName)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");
            if (fullNameClaim is not null)
                await _userManager.ReplaceClaimAsync(user, fullNameClaim, new System.Security.Claims.Claim("FullName", user.FullName));
            else
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", user.FullName));
        }

        await _signInManager.RefreshSignInAsync(user);
        return (true, "Configurações salvas com sucesso.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string userId, PasswordFormViewModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, "Usuário não encontrado.");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        await _signInManager.RefreshSignInAsync(user);
        return (true, "Senha alterada com sucesso.");
    }
}
