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
builder.Services.Configure<SwapiSettings>(builder.Configuration.GetSection("SwapiSettings"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<StarshipSearchService>();
builder.Services.AddHttpClient<SwapiService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<DetailsHelper>();
builder.Services.AddScoped<StarshipQueryHelper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Database Migration, Seeding, and AI Index Building at Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var searchService = services.GetRequiredService<StarshipSearchService>(); 

        // Safely run async database operations during synchronous app startup
        Task.Run(async () =>
        {
            // 1. Make sure the DB exists and is on the latest migration
            await context.Database.MigrateAsync();

            // 2. Seed data if necessary
            await context.SeedDataAsync();
            await searchService.BuildIndexAsync();

        }).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during startup: {ex.Message}");
    }
}

app.Run();