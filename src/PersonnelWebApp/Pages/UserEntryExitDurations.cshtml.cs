using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelWebApp.Infrastructure.Dtos;
using PersonnelWebApp.Infrastructure.Service;

namespace PersonnelWebApp.Pages
{
    public class UserEntryExitDurationsModel : PageModel
    {
        private readonly IUserService _userService;

        public IEnumerable<GetUserEntryExitDurationsResponseDto> UserEntryExitDurations { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime BeginDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string UserName { get; set; }

        public UserEntryExitDurationsModel(IUserService userService)
        {
            _userService = userService;
        }

        public async void OnGetAsync()
        {
            var userEntryExitDurations = await _userService.GetUserEntryExitDurationsAsync(new GetUserEntryExitDurationsRequestDto()
            {
                BeginDate = BeginDate,
                EndDate = EndDate,
                UserName = UserName
            });

            UserEntryExitDurations = userEntryExitDurations;
        }
    }
}
