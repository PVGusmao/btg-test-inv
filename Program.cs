using Microsoft.EntityFrameworkCore;
using ProjetoBtg.Api.Data;
using ProjetoBtg.Api.Events;
using ProjetoBtg.Api.Models;
using ProjetoBtg.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new AccountStatusJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new AccountOperationJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "accounts.db");
builder.Services.AddDbContext<AccountDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<IAccountRepository, SqliteAccountRepository>();
builder.Services.AddScoped<INotificationRegistry, SqliteNotificationRegistry>();
builder.Services.AddScoped<IAccountLifecycleConsumer, FraudPreventionConsumer>();
builder.Services.AddScoped<IAccountLifecycleConsumer, CardConsumer>();
builder.Services.AddScoped<IAccountLifecyclePublisher, AccountLifecyclePublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
