namespace PersonnelWebApp.Infrastructure.Service;

using PersonnelWebApp.Infrastructure.Model;

using MongoDB.Driver;

using Microsoft.AspNetCore.Identity;

public interface IUserService
{
    (string, bool) LoginUser(string userName, string password);
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
        var personnel = _mongoPersonnelCollection.AsQueryable().Where(x => x.UserName == userName).Select(x => new { x.UserName, x.Password, x.IsDeleted }).SingleOrDefault();
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

        return (string.Empty, true);
    }
}