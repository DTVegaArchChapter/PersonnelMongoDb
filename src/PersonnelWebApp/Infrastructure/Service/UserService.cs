namespace PersonnelWebApp.Infrastructure.Service;

using PersonnelWebApp.Infrastructure.Model;

using MongoDB.Driver;

using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using System;

public interface IUserService
{
    Task<(string, bool)> LoginUser(string userName, string password);
    IList<Personnel> GetPersonnelList(int pageNumber, int pageSize);
    int GetCount();
    Personnel GetPersonnel(string id);
    Task AddOrUpdatePersonnel(Personnel personnel);
    Task AddPersonnelVacation(Vacation vacation, string userName);
    (IEnumerable<Vacation> Data, int Count) GetUserVacations(int pageNumber, int pageSize, string userName);
    Task DeletePersonnelVacation(string userName, string vacationId);
}

public sealed class UserService : IUserService
{
    private readonly IMongoCollection<Personnel> _mongoPersonnelCollection;

    private readonly IPasswordHasher<string> _passwordHasher;

    private readonly IDateTimeProvider _dateTimeProvider;

    public UserService(IMongoCollection<Personnel> mongoPersonnelCollection, IPasswordHasher<string> passwordHasher, IDateTimeProvider dateTimeProvider)
    {
        _mongoPersonnelCollection = mongoPersonnelCollection ?? throw new ArgumentNullException(nameof(mongoPersonnelCollection));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<(string, bool)> LoginUser(string userName, string password)
    {
        var personnel = _mongoPersonnelCollection.AsQueryable().Where(x => x.UserName == userName).Select(x => new { x.Id, x.UserName, x.Password, x.Vacations, x.IsDeleted }).SingleOrDefault();
        if (personnel == null)
        {
            return ("User not found", false);
        }

        if (_passwordHasher.VerifyHashedPassword(userName, personnel.Password, password) == PasswordVerificationResult.Failed)
        {
            return ("Invalid password", false);
        }

        if (personnel.IsDeleted)
        {
            return ("User is not active", false);
        }

        if (personnel.Vacations != null)
        {
            DateTime now = DateTime.UtcNow;

            foreach (var vacation in personnel.Vacations)
            {
                if (now >= vacation.StartDate && now <= vacation.EndDate)
                {
                    return ("You are currently on vacation. Access denied.", false);
                }
            }
        }

        await AddPersonnelEntryExitHours(personnel.Id);

        return (string.Empty, true);
    }

    public IList<Personnel> GetPersonnelList(int pageNumber, int pageSize)
    {
        var personnelList = _mongoPersonnelCollection.AsQueryable().OrderBy(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return personnelList;
    }

    public int GetCount()
    {
        return _mongoPersonnelCollection.AsQueryable().Count();
    }

    public Personnel GetPersonnel(string id)
    {
        return _mongoPersonnelCollection.AsQueryable().FirstOrDefault(x => x.Id == id);
    }

    public async Task AddOrUpdatePersonnel(Personnel personnel)
    {
        personnel.Password = _passwordHasher.HashPassword(personnel.UserName, personnel.Password);

        if (!_mongoPersonnelCollection.AsQueryable().Any(x => x.UserName == personnel.UserName))
        {
            if (string.IsNullOrWhiteSpace(personnel.Id))
            {
                personnel.EntryExitHours = new List<EntryExitHour>();
                await _mongoPersonnelCollection.InsertOneAsync(personnel);
            }
            else
            {
                await _mongoPersonnelCollection.ReplaceOneAsync(x => x.Id == personnel.Id, personnel);
            }
        }
    }

    public (IEnumerable<Vacation> Data, int Count) GetUserVacations(int pageNumber, int pageSize, string userName)
    {
        var query = _mongoPersonnelCollection.AsQueryable().Where(x => x.UserName == userName);

        var test = _mongoPersonnelCollection.AsQueryable().ToList();

        var vacations = query.SelectMany(x => x.Vacations)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var count = query.SelectMany(x => x.Vacations)
            .Count();

        return (vacations, count);
    }

    public async Task AddPersonnelVacation(Vacation vacation, string userName)
    {
        vacation.Id = ObjectId.GenerateNewId().ToString();
        var filter = Builders<Personnel>.Filter.Eq(x => x.UserName, userName);
        var update = Builders<Personnel>.Update.Push(x => x.Vacations, vacation);

        await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
    }

    public async Task DeletePersonnelVacation(string userName, string vacationId)
    {
        var filter = Builders<Personnel>.Filter.And(
        Builders<Personnel>.Filter.Eq(x => x.UserName, userName),
        Builders<Personnel>.Filter.ElemMatch(x => x.Vacations, Builders<Vacation>.Filter.Eq(x => x.Id, vacationId))
    );

        var update = Builders<Personnel>.Update.PullFilter(x => x.Vacations, Builders<Vacation>.Filter.Eq(x => x.Id, vacationId));

        var result = await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
    }

    public async Task AddPersonnelEntryExitHours(string personnelId)
    {
        var lastEntryExitHours = _mongoPersonnelCollection.AsQueryable()
            .SelectMany(x => x.EntryExitHours).FirstOrDefault(x => x.EntryDate.Date == _dateTimeProvider.GetUtcNow().Date && x.IsDeleted == false);

        if (lastEntryExitHours != null)
        {
            if (lastEntryExitHours.ReasonType != Constant.PersonnelExitReasonType.WorkHour)
            {
                //TODO: Burcu - Close Meal Or Break Entry Exit Record
                //var filter = Builders<Personnel>.Filter.Where(x => x.Id == personnelId && x.EntryExitHours.Any(y => y.Id == lastEntryExitHours.Id));
                //var update = Builders<Personnel>.Update.Set(x => x.EntryExitHours[x.EntryExitHours.Count - 1].EntryDate, _dateTimeProvider.GetUtcNow()).Set(x => x.EntryExitHours[x.EntryExitHours.Count - 1].IsDeleted, true);

                //await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
                //await AddPersonnelEntryExitWorkHour(personnelId);
            }
        }
        else
        {
            await AddPersonnelEntryExitWorkHour(personnelId);
        }
    }

    private async Task AddPersonnelEntryExitWorkHour(string personnelId)
    {
        var filter = Builders<Personnel>.Filter.Eq(x => x.Id, personnelId);
        var update = Builders<Personnel>.Update.Push(
            x => x.EntryExitHours,
            new EntryExitHour
                {
                    Id = ObjectId.GenerateNewId().ToString(), ReasonType = Constant.PersonnelExitReasonType.WorkHour, EntryDate = _dateTimeProvider.GetUtcNow()
                });

        await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
    }
}