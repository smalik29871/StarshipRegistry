using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarshipRegistry.Configuration;
using StarshipRegistry.Data;

namespace StarshipRegistry.Tests;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var swapiSettings = Options.Create(new SwapiSettings { BaseUrl = "https://swapi.info/api/" });
        return new ApplicationDbContext(options, swapiSettings);
    }
}
