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
    private readonly IMongoCollection<Personnel> _mongoPersonnelCollection;

    private readonly IPasswordHasher<string> _passwordHasher;

    public DbInitializationService(IMongoCollection<Personnel> mongoPersonnelCollection, IPasswordHasher<string> passwordHasher)
    {
        _mongoPersonnelCollection = mongoPersonnelCollection ?? throw new ArgumentNullException(nameof(mongoPersonnelCollection));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public void InitDb()
    {
        if (_mongoPersonnelCollection.CountDocuments(FilterDefinition<Personnel>.Empty) == 0)
        {
            _mongoPersonnelCollection.InsertOne(new Personnel
                                              {
                                                  UserName = "admin",
                                                  Password = _passwordHasher.HashPassword("admin", "admin")
                                              });
        }
    }
}