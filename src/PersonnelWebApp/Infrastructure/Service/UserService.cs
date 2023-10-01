namespace PersonnelWebApp.Infrastructure.Service;

using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PersonnelWebApp.Infrastructure.Dtos;
using PersonnelWebApp.Infrastructure.Model;
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
    Task<IEnumerable<GetUserEntryExitDurationsResponseDto>> GetUserEntryExitDurationsAsync(GetUserEntryExitDurationsRequestDto request);
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
        var entryExitHours = _mongoPersonnelCollection.AsQueryable()
            .SelectMany(x => x.EntryExitHours);
        if (!await IAsyncCursorSourceExtensions.AnyAsync(entryExitHours))
        {
            var filter = Builders<Personnel>.Filter.Eq(x => x.Id, personnelId);
            var update = Builders<Personnel>.Update.Set(
                x => x.EntryExitHours,
                new List<EntryExitHour>());

            await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
            await AddPersonnelEntryExitWorkHour(personnelId);
        }
        else
        {
            var lastEntryExitHours = entryExitHours.Where(x => (x.EntryDate.Date == _dateTimeProvider.GetUtcNow().Date || x.ExitDate.Date == _dateTimeProvider.GetUtcNow().Date) && x.IsDeleted == false).OrderByDescending(x => x.CreateDate).FirstOrDefault();

            if (lastEntryExitHours != null)
            {
                if (lastEntryExitHours.ReasonType != Constant.PersonnelExitReasonType.WorkHour)
                {
                    var filter = Builders<Personnel>.Filter.Where(x => x.Id == personnelId && x.EntryExitHours.Any(y => y.Id == lastEntryExitHours.Id));
                    var update = Builders<Personnel>.Update.Set("EntryExitHours.$.EntryDate", _dateTimeProvider.GetUtcNow()).Set("EntryExitHours.$.IsDeleted", true);

                    await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
                }
            }
            else
            {
                await AddPersonnelEntryExitWorkHour(personnelId);
            }
        }
    }

    public async Task<IEnumerable<GetUserEntryExitDurationsResponseDto>> GetUserEntryExitDurationsAsync(GetUserEntryExitDurationsRequestDto request)
    {
        //var data = _mongoPersonnelCollection.AsQueryable()
        //    .SelectMany(m => m.EntryExitHours, (m, EntryExitHours) => new
        //    {
        //        m.UserName,
        //        EntryExitHours
        //    })
        //    .Where(m => m.EntryExitHours.EntryDate >= beginDate.Date && m.EntryExitHours.ExitDate <= endDate.Date)
        //    .GroupBy(m => new
        //    {
        //        m.UserName,
        //        m.EntryExitHours.ReasonType,
        //        EntryDate = m.EntryExitHours.EntryDate.ToString("yyyy-mm-dd")
        //    })
        //    .Select(m => new
        //    {
        //        TotalMinutes = m.Sum(x => (x.EntryExitHours.ExitDate - x.EntryExitHours.EntryDate).Minutes)
        //    })
        //    .ToList();


        var pipeline = new List<BsonDocument>()
        {
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$EntryExitHours" }
            }),

            new BsonDocument("$match", new BsonDocument
            {
                { "EntryExitHours.EntryDate", new BsonDocument { 
                    { "$gte", request.BeginDate.Date }, { "$lt", request.EndDate.Date } 
                } }
            }),

            new BsonDocument("$group", new BsonDocument {
                { "_id", new BsonDocument
                    {
                        { "_id", "$_id" },
                        { "UserName", "$UserName" },
                        { "ReasonType", "$EntryExitHours.ReasonType" },
                        { "EntryDate",  new BsonDocument {
                            { "$dateToString", new BsonDocument { 
                                { "format", "%Y-%m-%d" }, { "date", "$EntryExitHours.EntryDate" }
                            } }
                        } },
                    }
                },
                { "TotalMinutes", new BsonDocument {
                    { "$sum", new BsonDocument {
                        { "$dateDiff", new BsonDocument { 
                            { "startDate", "$EntryExitHours.EntryDate" },
                            { "endDate", "$EntryExitHours.ExitDate" },
                            { "unit", "minute" }
                        } }
                    } }
                } }
            }),

            new BsonDocument("$group", new BsonDocument { 
                { "_id", "$_id._id" },
                { "UserName", new BsonDocument { { "$first", "$_id.UserName" } } },
                { "EntryExitDurations", new BsonDocument { 
                    { "$push", new BsonDocument { 
                        { "EntryDate", "$_id.EntryDate" },
                        { "ReasonType", "$_id.ReasonType" },
                        { "TotalMinutes", "$TotalMinutes" },
                    } } 
                } },
            }),

            new BsonDocument("$project", new BsonDocument
            {
                { "_id", "$_id" },
                { "UserName", "$UserName" },
                { "EntryExitDurations", new BsonDocument { 
                    { "$sortArray", new BsonDocument { 
                        { "input", "$EntryExitDurations" },
                        { "sortBy", new BsonDocument { 
                            { "EntryDate", -1 },
                            { "ReasonType", 1 },
                        } }
                    } } 
                } },
            }),

            new BsonDocument("$sort", new BsonDocument
            {
                { "UserName", 1 }
            }),
        };

        var result = await _mongoPersonnelCollection
            .Aggregate((PipelineDefinition<Personnel, GetUserEntryExitDurationsResponseDto>)pipeline)
            .ToListAsync();


        return result;
    }

    private async Task AddPersonnelEntryExitWorkHour(string personnelId)
    {
        var filter = Builders<Personnel>.Filter.Eq(x => x.Id, personnelId);
        var update = Builders<Personnel>.Update.Push(
            x => x.EntryExitHours,
            new EntryExitHour
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ReasonType = Constant.PersonnelExitReasonType.WorkHour,
                EntryDate = _dateTimeProvider.GetUtcNow(),
                CreateDate = _dateTimeProvider.GetUtcNow()
            });

        await _mongoPersonnelCollection.UpdateOneAsync(filter, update);
    }
}