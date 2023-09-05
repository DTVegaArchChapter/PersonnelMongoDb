namespace PersonnelWebApp.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using PersonnelWebApp.Infrastructure.Service;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public string UserName { get; set; }

    [BindProperty, DataType(DataType.Password)]
    public string Password { get; set; }

    public string Message { get; set; }

    public IndexModel(IUserService userService)
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