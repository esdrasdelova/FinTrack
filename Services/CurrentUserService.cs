using System.Security.Claims;
using FinTrack.Data;
using FinTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    string FullName { get; }
    bool IsAuthenticated { get; }
    Task<ApplicationUser?> GetProfileAsync();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ApplicationDbContext _db;

    public CurrentUserService(IHttpContextAccessor accessor, ApplicationDbContext db)
    {
        _accessor = accessor;
        _db = db;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string UserName => User?.Identity?.Name ?? string.Empty;
    public string FullName => User?.FindFirstValue("FullName") ?? UserName;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Task<ApplicationUser?> GetProfileAsync() =>
        string.IsNullOrEmpty(UserId)
            ? Task.FromResult<ApplicationUser?>(null)
            : _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == UserId);
}
