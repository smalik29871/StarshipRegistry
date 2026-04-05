using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using System.Text.Json;

namespace StarshipRegistry.Tests;

public class StarshipQueryHelperTests
{
    private static StarshipQueryHelper CreateHelper()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var httpFactory = new Moq.Mock<System.Net.Http.IHttpClientFactory>();
        var config = new Moq.Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["Groq:ApiKey"]).Returns("test-key");
        config.Setup(c => c["Groq:Model"]).Returns("llama-3.1-8b-instant");
        config.Setup(c => c["Groq:BaseUrl"]).Returns("https://api.groq.com/openai/v1/chat/completions");
        return new StarshipQueryHelper(httpFactory.Object, config.Object, context);
    }

    [Fact]
    public void MapToRows_MapsAllFieldsCorrectly()
    {
        var helper = CreateHelper();
        var ships = new List<Starship>
        {
            new()
            {
                Url = "https://swapi.info/api/starships/9/",
                Name = "Death Star",
                Model = "DS-1 Orbital Battle Station",
                StarshipClass = "Deep Space Mobile Battlestation",
                CostInCredits = "1000000000000",
                Crew = "342953",
                HyperdriveRating = "4.0",
                Created = new DateTime(2014, 12, 10)
            }
        };

        var rows = helper.MapToRows(ships);
        var json = JsonSerializer.Serialize(rows[0]);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("9", root.GetProperty("id").GetString());
        Assert.Equal("Death Star", root.GetProperty("name").GetString());
        Assert.Equal("DS-1 Orbital Battle Station", root.GetProperty("model").GetString());
        Assert.Equal("1000000000000", root.GetProperty("costInCredits").GetString());
        Assert.Equal("342953", root.GetProperty("crew").GetString());
        Assert.Equal("4.0", root.GetProperty("hyperdriveRating").GetString());
        Assert.Equal("2014-12-10", root.GetProperty("created").GetString());
    }

    [Fact]
    public void MapToRows_ReturnsNAForNullFields()
    {
        var helper = CreateHelper();
        var ships = new List<Starship>
        {
            new()
            {
                Url = "https://swapi.info/api/starships/5/",
                Name = "Sentinel-class landing craft",
                Model = "Sentinel",
                StarshipClass = "landing craft",
                CostInCredits = null,
                Crew = null,
                HyperdriveRating = null,
                Created = null
            }
        };

        var rows = helper.MapToRows(ships);
        var json = JsonSerializer.Serialize(rows[0]);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("N/A", root.GetProperty("costInCredits").GetString());
        Assert.Equal("N/A", root.GetProperty("crew").GetString());
        Assert.Equal("N/A", root.GetProperty("hyperdriveRating").GetString());
        Assert.Equal("N/A", root.GetProperty("created").GetString());
    }

    [Fact]
    public void MapToRows_ReturnsEmptyListForNoShips()
    {
        var helper = CreateHelper();
        var rows = helper.MapToRows(new List<Starship>());
        Assert.Empty(rows);
    }

    [Fact]
    public void MapToRows_ExtractsIdFromUrl()
    {
        var helper = CreateHelper();
        var ships = new List<Starship>
        {
            new() { Url = "https://swapi.info/api/starships/12/", Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter" }
        };

        var rows = helper.MapToRows(ships);
        var json = JsonSerializer.Serialize(rows[0]);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("12", doc.RootElement.GetProperty("id").GetString());
    }
}
