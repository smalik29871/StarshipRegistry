using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using System.Text.Json;

namespace StarshipRegistry.Tests;

public class StarshipQueryHelperTests
{
    private static StarshipQueryHelper CreateHelper(
        StarshipRegistry.Data.ApplicationDbContext context,
        string apiKey = "",
        string groqUrl = "")
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler())
        {
            BaseAddress = new Uri("https://api.groq.com/")
        };

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["Groq:ApiKey"]).Returns(apiKey);
        config.Setup(c => c["Groq:Model"]).Returns("llama-3.1-8b-instant");
        config.Setup(c => c["Groq:BaseUrl"]).Returns(groqUrl);

        return new StarshipQueryHelper(
            httpFactory.Object,
            config.Object,
            context,
            NullLogger<StarshipQueryHelper>.Instance);
    }

    [Fact]
    public async Task ParseQueryAsync_returns_a_concept_search_when_groq_is_not_configured()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var helper = CreateHelper(context);

        var command = await helper.ParseQueryAsync("fast freighter");

        Assert.Equal("fast freighter", command.Concept);
        Assert.Equal(string.Empty, command.SortBy);
        Assert.Equal(10, command.Take);
    }

    [Fact]
    public async Task ExecuteQueryAsync_sorts_numeric_fields_descending()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.AddRange(
            new Starship { Url = "https://swapi.info/api/starships/9/", Name = "Death Star", Model = "DS-1", StarshipClass = "Battlestation", Crew = "342953" },
            new Starship { Url = "https://swapi.info/api/starships/10/", Name = "Millennium Falcon", Model = "YT-1300", StarshipClass = "Freighter", Crew = "4" },
            new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter", Crew = "1" });
        context.SaveChanges();

        var helper = CreateHelper(context);

        var results = await helper.ExecuteQueryAsync(new SearchCommand
        {
            SortBy = "crew",
            Order = "desc",
            Take = 2
        });

        Assert.Equal(2, results.Count);
        Assert.Equal("Death Star", results[0].Name);
        Assert.Equal("Millennium Falcon", results[1].Name);
    }

    [Fact]
    public async Task ExecuteQueryAsync_skips_unknown_values_before_sorting()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        context.Starships.AddRange(
            new Starship { Url = "https://swapi.info/api/starships/10/", Name = "Millennium Falcon", Model = "YT-1300", StarshipClass = "Freighter", HyperdriveRating = "0.5" },
            new Starship { Url = "https://swapi.info/api/starships/12/", Name = "X-wing", Model = "T-65 X-wing", StarshipClass = "Starfighter", HyperdriveRating = "1.0" },
            new Starship { Url = "https://swapi.info/api/starships/22/", Name = "Unknown Ship", Model = "Prototype", StarshipClass = "Testbed", HyperdriveRating = "unknown" });
        context.SaveChanges();

        var helper = CreateHelper(context);

        var results = await helper.ExecuteQueryAsync(new SearchCommand
        {
            SortBy = "hyperdrive",
            Order = "asc",
            Take = 10
        });

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, ship => ship.Name == "Unknown Ship");
        Assert.Equal("Millennium Falcon", results[0].Name);
    }

    [Fact]
    public void MapToRows_formats_values_for_the_grid()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var helper = CreateHelper(context);

        var rows = helper.MapToRows(new List<Starship>
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
        });

        var json = JsonSerializer.Serialize(rows[0]);
        using var doc = JsonDocument.Parse(json);
        var row = doc.RootElement;

        Assert.Equal("9", row.GetProperty("id").GetString());
        Assert.Equal("Death Star", row.GetProperty("name").GetString());
        Assert.Equal("1000000000000", row.GetProperty("costInCredits").GetString());
        Assert.Equal("342953", row.GetProperty("crew").GetString());
        Assert.Equal("4.0", row.GetProperty("hyperdriveRating").GetString());
        Assert.Equal("2014-12-10", row.GetProperty("created").GetString());
    }
}
