namespace PersonnelWebApp.Infrastructure.Service;

using PersonnelWebApp.Infrastructure.Model;

using MongoDB.Driver;

using Microsoft.AspNetCore.Identity;

public interface IDbInitializationService
{
    void InitDb();
}

public sealed class DbInitializationService : IDbInitializationService
{
    private readonly IMongoCollection<Personnel> _personnelCollection;

    private readonly IPasswordHasher<string> _passwordHasher;

    public DbInitializationService(IMongoCollection<Personnel> personnelCollection, IPasswordHasher<string> passwordHasher)
    {
        _personnelCollection = personnelCollection ?? throw new ArgumentNullException(nameof(personnelCollection));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public void InitDb()
    {
        if (_personnelCollection.CountDocuments(FilterDefinition<Personnel>.Empty) == 0)
        {
            _personnelCollection.InsertOne(new Personnel
                                              {
                                                  UserName = "admin",
                                                  Password = _passwordHasher.HashPassword("admin", "admin")
                                              });
        }
    }
}