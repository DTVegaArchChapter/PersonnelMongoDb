using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelWebApp.Infrastructure.Model;
using PersonnelWebApp.Infrastructure.Service;

namespace PersonnelWebApp.Pages
{
    public class VacationModel : PageModel
    {
        private readonly IUserService _userService;
        [BindProperty]
        public Vacation Vacation { get; set; }
        public VacationModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int Count { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling(decimal.Divide(Count, PageSize));
        public IEnumerable<Vacation> Data { get; set; }




        public void OnGetAsync()
        {
            var userName = HttpContext.User.Identity.Name;
            var response = _userService.GetUserVacations(CurrentPage, PageSize, userName);
            Data = response.Data;
            Count = response.Count;
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("ModelStateError", "Check the values you entered");
                return Page();
            }

            var userName = HttpContext.User.Identity.Name;

            var vacation = new Vacation
            {
                StartDate = Vacation.StartDate,
                EndDate = Vacation.EndDate,
            };

            await _userService.AddPersonelVacation(vacation, userName);

            return Redirect("Vacation");
        }

        public async Task<IActionResult> OnGetDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var userName = HttpContext.User.Identity.Name;

            await _userService.DeletePersonelVacation(userName, id);

            return RedirectToPage("Vacation");
        }

    }
}
