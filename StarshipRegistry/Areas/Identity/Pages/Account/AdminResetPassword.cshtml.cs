using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace StarshipRegistry.Areas.Identity.Pages.Account
{
    [Authorize]
    public class AdminResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminResetPasswordModel> _logger;

        public AdminResetPasswordModel(
            UserManager<IdentityUser> userManager,
            ILogger<AdminResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ResetLink { get; set; }
        public bool UserNotFound { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Agent Email")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                _logger.LogWarning("Admin {Admin} requested a reset link for unknown email {Email}.",
                    User.Identity?.Name, Input.Email);
                UserNotFound = true;
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            ResetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", email = Input.Email, code = encodedToken },
                protocol: Request.Scheme);

            _logger.LogInformation("Password reset link generated for {Email} by admin {Admin}.",
                Input.Email, User.Identity?.Name);

            return Page();
        }
    }
}
