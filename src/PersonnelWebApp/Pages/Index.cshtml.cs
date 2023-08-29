namespace PersonnelWebApp.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

using MongoDB.Driver;

using PersonnelWebApp.Infrastructure.Model;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IMongoCollection<Personnel> _mongoPersonnelCollection;

    private readonly IPasswordHasher<string> _passwordHasher;

    [BindProperty]
    public string UserName { get; set; }

    [BindProperty, DataType(DataType.Password)]
    public string Password { get; set; }

    public string Message { get; set; }

    public IndexModel(IMongoCollection<Personnel> mongoPersonnelCollection, IPasswordHasher<string> passwordHasher)
    {
        _mongoPersonnelCollection = mongoPersonnelCollection ?? throw new ArgumentNullException(nameof(mongoPersonnelCollection));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<IActionResult> OnPost()
    {
        var personnel =_mongoPersonnelCollection.AsQueryable().Where(x => x.UserName == UserName).Select(x => new { x.UserName, x.Password }).SingleOrDefault();
        if (personnel == null)
        {
            Message = "User not found";
            return Page();
        }

        if (_passwordHasher.VerifyHashedPassword(UserName, personnel.Password, Password) == PasswordVerificationResult.Failed)
        {
            Message = "Invalid password";
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new(ClaimTypes.Name, UserName) }, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToPage("/privacy");
    }
}