using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

using MongoDB.Driver;

using PersonnelWebApp.Infrastructure.Model;
using PersonnelWebApp.Infrastructure.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var mongoUrl = MongoUrl.Create(builder.Configuration.GetConnectionString("PersonnelDb"));

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl));
builder.Services.AddSingleton(x => x.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));
builder.Services.AddSingleton(x => x.GetRequiredService<IMongoDatabase>().GetCollection<Personnel>(nameof(Personnel)));
builder.Services.AddSingleton<IPasswordHasher<string>>(_ => new PasswordHasher<string>());
builder.Services.AddSingleton<IDbInitializationService, DbInitializationService>();
builder.Services.AddSingleton<IUserService, UserService>();

builder.Services.Configure<CookiePolicyOptions>(
    options =>
        {
            options.CheckConsentNeeded = _ => true;
        });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(
    cookieOptions =>
        {
            cookieOptions.LoginPath = "/";
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Services.GetRequiredService<IDbInitializationService>().InitDb();

app.Run();
