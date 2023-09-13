using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelWebApp.Infrastructure.Model;
using PersonnelWebApp.Infrastructure.Service;

namespace PersonnelWebApp.Pages
{
    public class AddOrUpdatePersonnelModel : PageModel
    {
        private readonly IUserService _userService;

        [BindProperty]
        public Personnel Personnel { get; set; }

        public AddOrUpdatePersonnelModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public void OnGet(string id)
        {
            Personnel = !string.IsNullOrWhiteSpace(id) ? _userService.GetPersonnel(id) : new Personnel();

            Personnel.Password = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(Personnel.Password))
            {
                await _userService.AddOrUpdatePersonnel(Personnel);
            
                return Redirect("PersonnelManagement");
            }
            else
            {
                ModelState.AddModelError("PasswordNull", "Password is empty");
                return Page();
            }
        }
    }
}
