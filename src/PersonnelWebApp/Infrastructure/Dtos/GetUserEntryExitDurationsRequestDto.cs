namespace PersonnelWebApp.Infrastructure.Dtos
{
    public class GetUserEntryExitDurationsRequestDto
    {
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UserName { get; set; }
    }
}
