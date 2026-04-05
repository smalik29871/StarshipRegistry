using Microsoft.AspNetCore.Mvc;
using Moq;
using StarshipRegistry.Models;
using StarshipRegistry.Models.ViewModels;

namespace StarshipRegistry.Tests;

public class StarshipControllerCrudTests
{
    [Fact]
    public async Task Details_returns_not_found_when_ship_is_missing()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var (controller, _) = StarshipControllerTestFactory.Create(context);

        var result = await controller.Details("999");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_returns_view_model_with_related_names()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        var filmUrl = "https://swapi.info/api/films/1/";
        var pilotUrl = "https://swapi.info/api/people/1/";
        var shipUrl = "https://swapi.info/api/starships/12";

        context.Films.Add(new Film { Url = filmUrl, Title = "A New Hope", EpisodeId = 4 });
        context.Characters.Add(new Character { Url = pilotUrl, Name = "Luke Skywalker" });
        context.Starships.Add(new Starship
        {
            Url = shipUrl,
            Name = "X-wing",
            Model = "T-65 X-wing",
            StarshipClass = "Starfighter",
            Films = new List<string> { filmUrl },
            Pilots = new List<string> { pilotUrl }
        });
        context.SaveChanges();

        var (controller, _) = StarshipControllerTestFactory.Create(context);

        var result = await controller.Details("12") as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("Details", result!.ViewName);

        var model = Assert.IsType<StarshipDetailsViewModel>(result.Model);
        Assert.Equal("X-wing", model.Starship.Name);
        Assert.Equal("A New Hope", model.FilmNames[filmUrl]);
        Assert.Equal("Luke Skywalker", model.PilotNames[pilotUrl]);
    }

    [Fact]
    public async Task Edit_returns_details_view_when_model_state_is_invalid()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Films.Add(new Film { Url = "https://swapi.info/api/films/1/", Title = "A New Hope", EpisodeId = 4 });
        context.Characters.Add(new Character { Url = "https://swapi.info/api/people/1/", Name = "Luke Skywalker" });
        context.SaveChanges();

        var (controller, _) = StarshipControllerTestFactory.Create(context);
        controller.ModelState.AddModelError("Name", "Name is required.");

        var ship = new Starship
        {
            Url = "https://swapi.info/api/starships/12/",
            Name = "",
            Model = "T-65 X-wing",
            StarshipClass = "Starfighter"
        };

        var result = await controller.Edit(ship, Array.Empty<string>(), Array.Empty<string>()) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("Details", result!.ViewName);
        Assert.IsType<StarshipDetailsViewModel>(result.Model);
    }

    [Fact]
    public async Task Edit_updates_selected_relations_and_redirects_to_details()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.Add(new Starship
        {
            Url = "https://swapi.info/api/starships/12/",
            Name = "X-wing",
            Model = "T-65",
            StarshipClass = "Starfighter"
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var (controller, _) = StarshipControllerTestFactory.Create(context);
        var updatedShip = new Starship
        {
            Url = "https://swapi.info/api/starships/12/",
            Name = "X-wing Mk II",
            Model = "T-65B",
            StarshipClass = "Starfighter"
        };

        var result = await controller.Edit(
            updatedShip,
            new[] { "https://swapi.info/api/people/1/" },
            new[] { "https://swapi.info/api/films/1/" }) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Details", result!.ActionName);
        Assert.Equal("12", result.RouteValues!["id"]);

        var savedShip = context.Starships.Single();
        Assert.Equal("X-wing Mk II", savedShip.Name);
        Assert.Contains("https://swapi.info/api/people/1/", savedShip.Pilots);
        Assert.Contains("https://swapi.info/api/films/1/", savedShip.Films);
    }

    [Fact]
    public async Task Edit_redirects_to_return_url_when_one_is_supplied()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.Add(new Starship
        {
            Url = "https://swapi.info/api/starships/12/",
            Name = "X-wing",
            Model = "T-65",
            StarshipClass = "Starfighter"
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var (controller, _) = StarshipControllerTestFactory.Create(context);
        var ship = new Starship
        {
            Url = "https://swapi.info/api/starships/12/",
            Name = "X-wing",
            Model = "T-65",
            StarshipClass = "Starfighter"
        };

        var result = await controller.Edit(ship, Array.Empty<string>(), Array.Empty<string>(), "/Starship") as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("/Starship", result!.Url);
    }

    [Fact]
    public async Task Delete_removes_a_matching_ship_and_rebuilds_the_index()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.Add(new Starship
        {
            Url = "https://swapi.info/api/starships/9/",
            Name = "Death Star",
            Model = "DS-1",
            StarshipClass = "Battlestation"
        });
        context.SaveChanges();

        var (controller, searchService) = StarshipControllerTestFactory.Create(context);

        var result = await controller.Delete("9") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result!.ActionName);
        Assert.Empty(context.Starships);
        searchService.Verify(service => service.BuildIndexAsync(), Times.Once);
    }

    [Fact]
    public async Task Delete_still_redirects_when_ship_is_not_found()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var (controller, searchService) = StarshipControllerTestFactory.Create(context);

        var result = await controller.Delete("999") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result!.ActionName);
        searchService.Verify(service => service.BuildIndexAsync(), Times.Never);
    }
}
