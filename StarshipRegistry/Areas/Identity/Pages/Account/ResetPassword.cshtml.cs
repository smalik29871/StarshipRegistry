using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace StarshipRegistry.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ResetPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool ShowSuccess { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            public string? Code { get; set; }
        }

        public IActionResult OnGet(string? email = null, string? code = null)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
                return BadRequest("A valid reset link is required.");

            Input = new InputModel { Email = email, Code = code };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrEmpty(Input.Code))
            {
                ModelState.AddModelError(string.Empty, "Invalid reset link.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal whether the account exists — show generic success
                ShowSuccess = true;
                return Page();
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty,
                    "This reset link appears to be corrupted. Request a new one from Imperial Command.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);

            if (result.Succeeded)
            {
                ShowSuccess = true;
                return Page();
            }

            foreach (var error in result.Errors.Where(e => e.Code is not "InvalidToken"))
                ModelState.AddModelError(string.Empty, error.Description);

            if (result.Errors.Any(e => e.Code == "InvalidToken"))
                ModelState.AddModelError(string.Empty,
                    "This reset link has expired or has already been used. Contact Imperial Command for a new one.");

            return Page();
        }
    }
}
