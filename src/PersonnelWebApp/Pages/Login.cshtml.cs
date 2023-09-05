using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelWebApp.Infrastructure.Service;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PersonnelWebApp.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;

        [BindProperty]
        public string UserName { get; set; }

        [BindProperty, DataType(DataType.Password)]
        public string Password { get; set; }

        public string Message { get; set; }

        public LoginModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task<IActionResult> OnPost()
        {
            var (message, success) = _userService.LoginUser(UserName, Password);
            if (!success)
            {
                Message = message;
                return Page();
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new(ClaimTypes.Name, UserName) }, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToPage("/privacy");
        }
    }
}
