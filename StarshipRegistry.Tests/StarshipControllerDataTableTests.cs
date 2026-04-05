using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using StarshipRegistry.Configuration;
using StarshipRegistry.Controllers;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using StarshipRegistry.Services;
using System.Text.Json;

namespace StarshipRegistry.Tests;

public class StarshipControllerDataTableTests
{
    private static (StarshipController controller, StarshipRegistry.Data.ApplicationDbContext context) CreateController()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        context.Starships.AddRange(
            new Starship { Url = "https://swapi.info/api/starships/9/",  Name = "Death Star",  Model = "DS-1",       StarshipClass = "battlestation", CostInCredits = "1000000000000", Crew = "342953" },
            new Starship { Url = "https://swapi.info/api/starships/10/", Name = "Millennium Falcon", Model = "YT-1300", StarshipClass = "freighter",    CostInCredits = "100000",       Crew = "4" },
            new Starship { Url = "https://swapi.info/api/starships/11/", Name = "Y-wing",      Model = "BTL Y-wing",  StarshipClass = "assault starfighter", CostInCredits = "134999", Crew = "2" },
            new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing",      Model = "T-65 X-wing", StarshipClass = "starfighter",   CostInCredits = "149999",       Crew = "1" }
        );
        context.SaveChanges();

        var httpFactory = new Mock<System.Net.Http.IHttpClientFactory>();
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["Groq:ApiKey"]).Returns("test-key");
        config.Setup(c => c["Groq:Model"]).Returns("llama-3.1-8b-instant");
        config.Setup(c => c["Groq:BaseUrl"]).Returns("https://api.groq.com/openai/v1/chat/completions");

        var queryHelper = new StarshipQueryHelper(httpFactory.Object, config.Object, context);
        var swapiSettings = Options.Create(new SwapiSettings { BaseUrl = "https://swapi.info/api/" });

        var controller = new StarshipController(
            null!,
            context,
            null!,
            null!,
            queryHelper,
            swapiSettings
        );

        return (controller, context);
    }

    [Fact]
    public async Task DataTable_ReturnsAllRecords_WhenNoSearch()
    {
        var (controller, _) = CreateController();
        var request = new DataTableRequest { Draw = 1, Start = 0, Length = 10 };

        var result = await controller.DataTable(request) as JsonResult;
        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(4, doc.RootElement.GetProperty("recordsTotal").GetInt32());
        Assert.Equal(4, doc.RootElement.GetProperty("recordsFiltered").GetInt32());
        Assert.Equal(4, doc.RootElement.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task DataTable_FiltersRecords_BySearchValue()
    {
        var (controller, _) = CreateController();
        var request = new DataTableRequest
        {
            Draw = 1, Start = 0, Length = 10,
            Search = new DataTableSearch { Value = "wing" }
        };

        var result = await controller.DataTable(request) as JsonResult;
        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(4, doc.RootElement.GetProperty("recordsTotal").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("recordsFiltered").GetInt32());
    }

    [Fact]
    public async Task DataTable_RespectsPageSize()
    {
        var (controller, _) = CreateController();
        var request = new DataTableRequest { Draw = 1, Start = 0, Length = 2 };

        var result = await controller.DataTable(request) as JsonResult;
        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.GetProperty("data").GetArrayLength());
        Assert.Equal(4, doc.RootElement.GetProperty("recordsTotal").GetInt32());
    }

    [Fact]
    public async Task DataTable_RespectsOffset()
    {
        var (controller, _) = CreateController();
        var request = new DataTableRequest { Draw = 1, Start = 2, Length = 10 };

        var result = await controller.DataTable(request) as JsonResult;
        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task DataTable_SortsByNameAscending()
    {
        var (controller, _) = CreateController();
        var request = new DataTableRequest
        {
            Draw = 1, Start = 0, Length = 10,
            Order = new[] { new DataTableOrder { Column = 0, Dir = "asc" } }
        };

        var result = await controller.DataTable(request) as JsonResult;
        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);

        var names = doc.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(r => r.GetProperty("name").GetString())
            .ToList();

        Assert.Equal("Death Star", names[0]);
        Assert.Equal("Y-wing", names[^1]);
    }

    [Fact]
    public async Task DataTable_ReturnsDraw_EchoedBack()
    {
        var (controller, _) = CreateController();
        var request = new DataTableRequest { Draw = 42, Start = 0, Length = 10 };

        var result = await controller.DataTable(request) as JsonResult;
        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(42, doc.RootElement.GetProperty("draw").GetInt32());
    }
}
