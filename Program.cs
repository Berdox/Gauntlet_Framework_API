using gauntlet_framework_api.authentication;
using gauntlet_framework_api.database;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 1. Configure EF Core with PostgreSQL & Snake Case Naming
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// 2. Register Custom API Key Authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, _ => { });

builder.Services.AddHttpLogging(logging => {
    logging.LoggingFields = HttpLoggingFields.RequestMethod
                          | HttpLoggingFields.RequestPath
                          | HttpLoggingFields.RequestQuery
                          | HttpLoggingFields.ResponseStatusCode
                          | HttpLoggingFields.Duration;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 4. Ensure DB schema is up to date on startup
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();