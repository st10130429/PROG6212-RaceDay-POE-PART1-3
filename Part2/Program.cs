using Microsoft.AspNetCore.Authentication;
using RaceDay.Api.Auth;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Database (SQL Server, Code-First) ---
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Server-side session authentication ---
// The brief calls for "role-based session authentication": on login the API stores the
// user's ID and role in the SERVER-SIDE session store. The client only holds an opaque
// session-id cookie ("RaceDay.Session"); every request is authenticated from the server's
// session by SessionAuthenticationHandler.
builder.Services.AddDistributedMemoryCache(); // server-side session storage
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddAuthentication(SessionAuthDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthDefaults.Scheme, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganiserOnly", policy => policy.RequireRole(Roles.Organiser));
    options.AddPolicy("ParticipantOnly", policy => policy.RequireRole(Roles.Participant));
});

builder.Services.AddControllers();

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API powering the RaceDay event management system (PROG6212 Part 2). " +
                      "Log in via /api/auth/login first - the session cookie Swagger receives is then " +
                      "sent automatically on every subsequent 'Try it out' call."
    });

    // Endpoint descriptions (purpose, expected input, response) come from the XML doc comments.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// Needed so WebApplicationFactory<Program> in the test project can boot this app.
builder.Services.AddCors();

var app = builder.Build();

// Auto-apply any pending EF Core migrations on startup (keeps DB schema in sync).
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();

    // Creates the database and tables from the models on first run, so the API works
    // straight away. If you later add EF migrations, they are applied instead.
    if (db.Database.GetMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

// Swagger is always on: the brief requires every endpoint to be viewable/testable in a browser.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

app.UseHttpsRedirection();

app.UseSession(); // must come before authentication: the session feeds the auth handler
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to the test project for WebApplicationFactory<Program>.
public partial class Program { }
