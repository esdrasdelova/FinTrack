using FinTrack.Data;
using FinTrack.Extensions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine("logs", "fintrack-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddFinTrackInfrastructure(builder.Configuration);
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AuthorizeFilter());
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
            await services.GetRequiredService<IDemoDataSeeder>().SeedAsync();
            Log.Information("Banco de dados pronto e dados de demonstração verificados.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Não foi possível inicializar o banco de dados. Verifique a connection string.");
        }
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    Log.Information("FinTrack iniciado.");
    app.Run();
}
catch (Exception ex) when (ex.GetType().Name == "HostAbortedException")
{
    // O tooling do EF Core interrompe o host de propósito ao criar o design-time host.
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha fatal ao iniciar o FinTrack.");
}
finally
{
    Log.CloseAndFlush();
}
