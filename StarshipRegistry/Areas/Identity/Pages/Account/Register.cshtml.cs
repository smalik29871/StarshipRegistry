using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace StarshipRegistry.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        private const int MaxAttemptsPerWindow = 5;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IConfiguration config,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _config = config;
            _cache = cache;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "An Imperial Access Code is required.")]
            [Display(Name = "Imperial Access Code")]
            public string RegistrationCode { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var honeypot = Request.Form["hpt_url"].ToString();
            if (!string.IsNullOrEmpty(honeypot))
            {
                _logger.LogWarning("Honeypot triggered on registration from IP {IP}.",
                    HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                return Page();
            }

            if (IsRateLimited())
            {
                _logger.LogWarning("Registration rate limit exceeded from IP {IP}.",
                    HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(string.Empty,
                    "Too many attempts. Stand down, soldier. Try again in 15 minutes.");
                return Page();
            }

            if (!ModelState.IsValid)
                return Page();

            var validCode = _config["Auth:RegistrationCode"];
            if (string.IsNullOrEmpty(validCode) ||
                !string.Equals(Input.RegistrationCode, validCode, StringComparison.Ordinal))
            {
                _logger.LogWarning("Invalid registration code attempt from IP {IP}.",
                    HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(nameof(Input.RegistrationCode),
                    "Invalid Imperial Access Code. Access denied.");
                return Page();
            }

            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("New Imperial agent registered.");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        private bool IsRateLimited()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"reg_attempt_{ip}";

            var count = _cache.Get<int>(key);
            if (count >= MaxAttemptsPerWindow)
                return true;

            _cache.Set(key, count + 1, new MemoryCacheEntryOptions
            {
                SlidingExpiration = RateLimitWindow
            });

            return false;
        }
    }
}
