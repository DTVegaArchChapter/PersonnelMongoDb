namespace PersonnelWebApp.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

[AllowAnonymous]
public class IndexModel : PageModel
{
    [BindProperty]
    public string UserName { get; set; }

    [BindProperty, DataType(DataType.Password)]
    public string Password { get; set; }

    public string Message { get; set; }

    public async Task<IActionResult> OnPost()
    {
        if (!string.IsNullOrWhiteSpace(UserName))
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new(ClaimTypes.Name, UserName) }, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToPage("/privacy");
        }

        Message = "Invalid User name or Password";
        return Page();
    }
}