using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StarshipRegistry.Models;

namespace StarshipRegistry.Tests;

public class StarshipControllerSeedTests
{
    [Fact]
    public async Task Seed_syncs_data_and_redirects_to_index()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        var handler = new TestHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.AbsoluteUri;
            var payload = url switch
            {
                "https://swapi.info/api/films" => "[]",
                "https://swapi.info/api/planets" => "[]",
                "https://swapi.info/api/people" => "[]",
                "https://swapi.info/api/species" => "[]",
                "https://swapi.info/api/starships" =>
                    """
                    [
                      {
                        "url": "https://swapi.info/api/starships/10/",
                        "name": "Millennium Falcon",
                        "model": "YT-1300 light freighter",
                        "manufacturer": "Corellian Engineering Corporation",
                        "cost_in_credits": "100000",
                        "length": "34.37",
                        "crew": "4",
                        "passengers": "6",
                        "hyperdrive_rating": "0.5",
                        "starship_class": "Light freighter",
                        "cargo_capacity": "100000",
                        "consumables": "2 months",
                        "MGLT": "75",
                        "max_atmosphering_speed": "1050",
                        "pilots": [],
                        "films": []
                      }
                    ]
                    """,
                _ => "[]"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });

        var (controller, searchService) = StarshipControllerTestFactory.Create(context, handler);

        var result = await controller.Seed() as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result!.ActionName);

        var savedShip = Assert.Single(context.Starships);
        Assert.Equal("Millennium Falcon", savedShip.Name);
        searchService.Verify(service => service.BuildIndexAsync(), Times.Once);
    }
}
