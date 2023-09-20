namespace PersonnelWebApp.Pages;

using PersonnelWebApp.Infrastructure.Service;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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

    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl)
    {
        var (message, success) = await _userService.LoginUser(UserName, Password);
        if (!success)
        {
            Message = message;
            return Page();
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, UserName)
                    }, 
                CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Redirect(returnUrl ?? "/");
    }
}