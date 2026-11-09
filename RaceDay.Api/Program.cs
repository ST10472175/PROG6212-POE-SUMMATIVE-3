using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//connect to the SQL Server database using Entity Framework Core
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//register the Azure Blob Storage helper used for event and profile images
builder.Services.AddScoped<BlobStorageService>();

//add the controllers and show enums (Run, Walk, Cycle) as text in the JSON
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//server-side session management
//the session stores the user id and role after the user logs in
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

//add Swagger so the endpoints can be viewed and tested in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for the RaceDay running, walking and cycling events system. " +
                      "Register and then log in first (POST /api/auth/login). " +
                      "The session cookie is kept by the browser, so all the other endpoints on this page use your login."
    });

    //use the /// comments in the controllers as the endpoint descriptions
    string xmlFile = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

WebApplication app = builder.Build();

//create the database tables when the application starts
using (IServiceScope scope = app.Services.CreateScope())
{
    RaceDayContext db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();

    // The supplied Part 2 source contains no committed migration files.
    // EnsureCreated makes the schema available for a clean LocalDB or Docker database
    // and is also compatible with the in-memory database used by the tests.
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

// The API is called server-to-server by the MVC app. Deployments should terminate
// public HTTPS at a reverse proxy and keep this internal container endpoint private.

//the session must be switched on before the controllers
app.UseSession();

app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    RaceDayContext db =
        scope.ServiceProvider.GetRequiredService<RaceDayContext>();

    db.Database.EnsureCreated();
}

app.Run();

//this line lets the unit test project start the application
public partial class Program { }
