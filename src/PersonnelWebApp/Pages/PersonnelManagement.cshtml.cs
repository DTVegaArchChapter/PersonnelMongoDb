namespace PersonnelWebApp.Pages;

using PersonnelWebApp.Infrastructure.Service;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using PersonnelWebApp.Infrastructure.Model;

public class PersonnelManagementModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int Count { get; set; }
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(decimal.Divide(Count, PageSize));
    public IList<Personnel> Data { get; set; }

    public PersonnelManagementModel(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task OnGetAsync()
    {
        Data = _userService.GetPersonnelList(CurrentPage, PageSize);
        Count = _userService.GetCount();
    }
}