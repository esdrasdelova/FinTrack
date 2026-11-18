using FinTrack.Data;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFinTrackInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=FinTrackDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 3;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ".FinTrack.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IInvestmentService, InvestmentService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IRecurringService, RecurringService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        return services;
    }
}
