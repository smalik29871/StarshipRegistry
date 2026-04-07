using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StarshipRegistry.Configuration;
using StarshipRegistry.Controllers;
using StarshipRegistry.Data;
using StarshipRegistry.Helpers;
using StarshipRegistry.Services;

namespace StarshipRegistry.Tests;

internal static class StarshipControllerTestFactory
{
    public static (StarshipController Controller, Mock<StarshipSearchService> SearchService) Create(
        ApplicationDbContext context,
        HttpMessageHandler? handler = null)
    {
        var httpClient = new HttpClient(handler ?? new TestHttpMessageHandler())
        {
            BaseAddress = new Uri("https://swapi.info/api/")
        };

        var swapiSettings = Options.Create(new SwapiSettings { BaseUrl = "https://swapi.info/api/" });
        var swapiService = new SwapiService(httpClient, context, NullLogger<SwapiService>.Instance, swapiSettings);

        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["Groq:ApiKey"]).Returns(string.Empty);
        config.Setup(c => c["Groq:Model"]).Returns("llama-3.1-8b-instant");
        config.Setup(c => c["Groq:BaseUrl"]).Returns(string.Empty);

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var queryHelper = new StarshipQueryHelper(
            httpFactory.Object,
            config.Object,
            context,
            NullLogger<StarshipQueryHelper>.Instance,
            Mock.Of<IMemoryCache>());

        var detailsHelper = new DetailsHelper(swapiService, context, swapiSettings);
        var searchService = new Mock<StarshipSearchService>(
            MockBehavior.Loose,
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
        searchService.Setup(service => service.BuildIndexAsync()).Returns(Task.CompletedTask);
        searchService.Setup(service => service.AddToIndexAsync(It.IsAny<StarshipRegistry.Models.Starship>())).Returns(Task.CompletedTask);
        searchService.Setup(service => service.RemoveFromIndex(It.IsAny<string>()));
        searchService.Setup(service => service.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<StarshipRegistry.Models.Starship>());

        var controllerCache = new MemoryCache(new MemoryCacheOptions());
        var controller = new StarshipController(
            swapiService,
            context,
            detailsHelper,
            searchService.Object,
            queryHelper,
            NullLogger<StarshipController>.Instance,
            swapiSettings,
            controllerCache);

        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>()))
            .Returns<string>(s => !string.IsNullOrEmpty(s) && s.StartsWith("/") && !s.StartsWith("//"));
        controller.Url = urlHelper.Object;

        return (controller, searchService);
    }
}
