using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarshipRegistry.Configuration;
using StarshipRegistry.Data;
using StarshipRegistry.Helpers;
using StarshipRegistry.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.Configure<SwapiSettings>(builder.Configuration.GetSection("SwapiSettings"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var isSqlite = connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
             && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);

// On Azure App Service, redirect relative SQLite paths to the persistent HOME directory
// so the database survives redeployments. WEBSITE_SITE_NAME is set by Azure App Service.
if (isSqlite && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
{
    var parts = connectionString
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(p => p.Trim())
        .ToList();
    var dsPart = parts.FirstOrDefault(p =>
        p.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
    if (dsPart is not null)
    {
        var file = dsPart["Data Source=".Length..].Trim();
        if (!Path.IsPathRooted(file))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? ".";
            parts[parts.IndexOf(dsPart)] = $"Data Source={Path.Combine(home, file)}";
            connectionString = string.Join(";", parts);
        }
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<StarshipSearchService>();
builder.Services.AddHttpClient<SwapiService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<DetailsHelper>();
builder.Services.AddScoped<StarshipQueryHelper>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var searchService = services.GetRequiredService<StarshipSearchService>();

        Task.Run(async () =>
        {
            if (isSqlite)
            {
                // /home won't exist on a fresh App Service deploy
                var dataSource = connectionString
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .FirstOrDefault(p => p.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    ?[12..].Trim();

                if (!string.IsNullOrEmpty(dataSource))
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                }

                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            await context.SeedDataAsync();

            try
            {
                await searchService.BuildIndexAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Vector index skipped: {ex.Message}");
            }

        }).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during startup: {ex.Message}");
    }
}

app.Run();