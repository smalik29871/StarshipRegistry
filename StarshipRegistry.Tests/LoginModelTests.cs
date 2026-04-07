using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarshipRegistry.Areas.Identity.Pages.Account;

namespace StarshipRegistry.Tests;

public class LoginModelTests
{
    // -----------------------------------------------------------------------
    // Factory helpers
    // -----------------------------------------------------------------------

    private static Mock<SignInManager<IdentityUser>> CreateSignInManagerMock()
    {
        var userStore = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        return new Mock<SignInManager<IdentityUser>>(
            userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object,
            null!, null!, null!, null!);
    }

    private static LoginModel CreatePageModel(Mock<SignInManager<IdentityUser>> signInManager)
    {
        var httpContext = new DefaultHttpContext();

        var authService = new Mock<IAuthenticationService>();
        authService
            .Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string?>(),
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(authService.Object)
            .BuildServiceProvider();

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            modelState);

        var pageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState)
        };

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Content("~/")).Returns("/");

        return new LoginModel(signInManager.Object, NullLogger<LoginModel>.Instance)
        {
            PageContext = pageContext,
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = urlHelper.Object
        };
    }

    // -----------------------------------------------------------------------
    // OnPostAsync tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsPage()
    {
        var signInManager = CreateSignInManagerMock();
        var model = CreatePageModel(signInManager);
        model.PageContext.ModelState.AddModelError("Email", "Required");

        var result = await model.OnPostAsync("/");

        Assert.IsType<PageResult>(result);
        signInManager.Verify(
            s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ValidCredentials_RedirectsToReturnUrl()
    {
        var signInManager = CreateSignInManagerMock();
        signInManager
            .Setup(s => s.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var model = CreatePageModel(signInManager);
        model.Input = new LoginModel.InputModel
        {
            Email = "vader@empire.com",
            Password = "DarkSide1",
            RememberMe = false
        };

        var result = await model.OnPostAsync("/starships");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/starships", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_ValidCredentials_CallsPasswordSignInWithCorrectCredentials()
    {
        var signInManager = CreateSignInManagerMock();
        signInManager
            .Setup(s => s.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var model = CreatePageModel(signInManager);
        model.Input = new LoginModel.InputModel
        {
            Email = "vader@empire.com",
            Password = "DarkSide1",
            RememberMe = true
        };

        await model.OnPostAsync("/");

        signInManager.Verify(
            s => s.PasswordSignInAsync("vader@empire.com", "DarkSide1", true, false),
            Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCredentials_ReturnsPageWithError()
    {
        var signInManager = CreateSignInManagerMock();
        signInManager
            .Setup(s => s.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var model = CreatePageModel(signInManager);
        model.Input = new LoginModel.InputModel { Email = "spy@rebel.com", Password = "WrongPass1" };

        var result = await model.OnPostAsync("/");

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(
            model.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "Invalid credentials. The Empire is watching.");
    }

    [Fact]
    public async Task OnPostAsync_LockedOutUser_RedirectsToLockoutPage()
    {
        var signInManager = CreateSignInManagerMock();
        signInManager
            .Setup(s => s.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var model = CreatePageModel(signInManager);
        model.Input = new LoginModel.InputModel { Email = "banned@empire.com", Password = "Password1" };

        var result = await model.OnPostAsync("/");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_NullReturnUrl_DefaultsToRoot()
    {
        var signInManager = CreateSignInManagerMock();
        signInManager
            .Setup(s => s.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var model = CreatePageModel(signInManager);
        model.Input = new LoginModel.InputModel { Email = "agent@empire.com", Password = "Password1" };

        var result = await model.OnPostAsync(null);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    // -----------------------------------------------------------------------
    // OnGetAsync tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task OnGetAsync_SetsReturnUrl()
    {
        var signInManager = CreateSignInManagerMock();
        var model = CreatePageModel(signInManager);

        await model.OnGetAsync("/starships");

        Assert.Equal("/starships", model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_NullReturnUrl_DefaultsToRoot()
    {
        var signInManager = CreateSignInManagerMock();
        var model = CreatePageModel(signInManager);

        await model.OnGetAsync(null);

        Assert.Equal("/", model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_WithErrorMessage_AddsModelError()
    {
        var signInManager = CreateSignInManagerMock();
        var model = CreatePageModel(signInManager);
        model.ErrorMessage = "Session expired. Please log in again.";

        await model.OnGetAsync();

        Assert.True(model.ModelState.ContainsKey(string.Empty));
        Assert.Contains(
            model.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "Session expired. Please log in again.");
    }

    [Fact]
    public async Task OnGetAsync_SignsOutExternalScheme()
    {
        var signInManager = CreateSignInManagerMock();
        var httpContext = new DefaultHttpContext();

        var authService = new Mock<IAuthenticationService>();
        authService
            .Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                IdentityConstants.ExternalScheme,
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(authService.Object)
            .BuildServiceProvider();

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManager.Object, NullLogger<LoginModel>.Instance)
        {
            PageContext = new PageContext(actionContext)
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState)
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = urlHelper.Object
        };

        await model.OnGetAsync();

        authService.Verify(
            a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties?>()),
            Times.Once);
    }
}
