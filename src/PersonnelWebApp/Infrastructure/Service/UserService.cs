namespace PersonnelWebApp.Infrastructure.Service;

using PersonnelWebApp.Infrastructure.Model;

using MongoDB.Driver;

using Microsoft.AspNetCore.Identity;

public interface IUserService
{
    (string, bool) LoginUser(string userName, string password);
    IList<Personnel> GetPersonnelList(int pageNumber, int pageSize);
    int GetCount();
    Personnel GetPersonnel(string id);
    Task AddOrUpdatePersonnel(Personnel personnel);
}

public sealed class UserService : IUserService
{
    private readonly IMongoCollection<Personnel> _mongoPersonnelCollection;

    private readonly IPasswordHasher<string> _passwordHasher;

    public UserService(IMongoCollection<Personnel> mongoPersonnelCollection, IPasswordHasher<string> passwordHasher)
    {
        _mongoPersonnelCollection = mongoPersonnelCollection ?? throw new ArgumentNullException(nameof(mongoPersonnelCollection));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public (string, bool) LoginUser(string userName, string password)
    {
        var personnel = _mongoPersonnelCollection.AsQueryable().Where(x => x.UserName == userName).Select(x => new { x.UserName, x.Password }).SingleOrDefault();
        if (personnel == null)
        {
            return ("User not found", false);
        }

        if (_passwordHasher.VerifyHashedPassword(userName, personnel.Password, password) == PasswordVerificationResult.Failed)
        {
            return ("Invalid password", false);
        }

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

                await _mongoPersonnelCollection.InsertOneAsync(personnel);
            }
            else
            {
                await _mongoPersonnelCollection.ReplaceOneAsync(x => x.Id == personnel.Id, personnel);
            }
        }
    }
}