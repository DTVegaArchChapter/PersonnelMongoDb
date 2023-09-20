namespace PersonnelWebApp.Infrastructure.Service;

public interface IDateTimeProvider
{
    DateTime GetUtcNow();
}

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }
}