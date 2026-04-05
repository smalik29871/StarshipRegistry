using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StarshipRegistry.Configuration;
using StarshipRegistry.Controllers;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using StarshipRegistry.Services;

namespace StarshipRegistry.Tests;

public class StarshipControllerCrudTests
{
    private const string BaseUrl = "https://swapi.info/api/";

    private static StarshipController CreateController(StarshipRegistry.Data.ApplicationDbContext context)
    {
        var httpFactory = new Mock<System.Net.Http.IHttpClientFactory>();
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["Groq:ApiKey"]).Returns("test-key");
        config.Setup(c => c["Groq:Model"]).Returns("llama-3.1-8b-instant");
        config.Setup(c => c["Groq:BaseUrl"]).Returns("https://api.groq.com/openai/v1/chat/completions");

        var queryHelper = new StarshipQueryHelper(httpFactory.Object, config.Object, context);
        var swapiSettings = Options.Create(new SwapiSettings { BaseUrl = BaseUrl });

        // Mock SearchService so Delete's BuildIndexAsync doesn't throw
        var searchService = new Mock<StarshipSearchService>(MockBehavior.Loose,
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
        searchService.Setup(s => s.BuildIndexAsync()).Returns(Task.CompletedTask);

        var controller = new StarshipController(null!, context, null!, searchService.Object, queryHelper, swapiSettings);

        // Wire up TempData so controller doesn't throw on TempData["Message"] = ...
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;

        return controller;
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidShip_SavesAndRedirectsToIndex()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var ship = new Starship { Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter" };
        var result = await controller.Create(ship, Array.Empty<string>(), Array.Empty<string>()) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result!.ActionName);
        Assert.Single(context.Starships);
    }

    [Fact]
    public async Task Create_GeneratesUrl_WhenUrlIsEmpty()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var ship = new Starship { Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter", Url = "" };
        await controller.Create(ship, Array.Empty<string>(), Array.Empty<string>());

        var saved = context.Starships.Single();
        Assert.Contains($"{BaseUrl}starships/", saved.Url);
    }

    [Fact]
    public async Task Create_SetsCreatedTimestamp()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var before = DateTime.UtcNow;
        var ship = new Starship { Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter" };
        await controller.Create(ship, Array.Empty<string>(), Array.Empty<string>());

        var saved = context.Starships.Single();
        Assert.NotNull(saved.Created);
        Assert.True(saved.Created >= before);
    }

    [Fact]
    public async Task Create_AssignsFilmsAndPilots()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var ship = new Starship { Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter" };
        var films = new[] { "https://swapi.info/api/films/1/" };
        var pilots = new[] { "https://swapi.info/api/people/1/" };
        await controller.Create(ship, films, pilots);

        var saved = context.Starships.Single();
        Assert.Contains("https://swapi.info/api/films/1/", saved.Films);
        Assert.Contains("https://swapi.info/api/people/1/", saved.Pilots);
    }

    // ── EDIT ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_ValidShip_UpdatesAndRedirectsToDetails()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.Add(new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing", Model = "T-65", StarshipClass = "Starfighter" });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var controller = CreateController(context);
        var updated = new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing MkII", Model = "T-65B", StarshipClass = "Starfighter" };

        var result = await controller.Edit(updated, Array.Empty<string>(), Array.Empty<string>()) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Details", result!.ActionName);
        Assert.Equal("X-wing MkII", context.Starships.Single().Name);
    }

    [Fact]
    public async Task Edit_WithReturnUrl_RedirectsToReturnUrl()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.Add(new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing", Model = "T-65", StarshipClass = "Starfighter" });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var controller = CreateController(context);
        var ship = new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing", Model = "T-65", StarshipClass = "Starfighter" };

        var result = await controller.Edit(ship, Array.Empty<string>(), Array.Empty<string>(), "/Starship") as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("/Starship", result!.Url);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingShip_RemovesAndRedirectsToIndex()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.Add(new Starship { Url = "https://swapi.info/api/starships/9/", Name = "Death Star", Model = "DS-1", StarshipClass = "battlestation" });
        context.SaveChanges();

        var controller = CreateController(context);
        var result = await controller.Delete("9") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result!.ActionName);
        Assert.Empty(context.Starships);
    }

    [Fact]
    public async Task Delete_NonExistentShip_StillRedirectsToIndex()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var result = await controller.Delete("999") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result!.ActionName);
    }
}
