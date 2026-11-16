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
builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
builder.Services.AddSingleton<INotificationRegistry, InMemoryNotificationRegistry>();
builder.Services.AddSingleton<IAccountLifecycleConsumer, FraudPreventionConsumer>();
builder.Services.AddSingleton<IAccountLifecycleConsumer, CardConsumer>();
builder.Services.AddSingleton<IAccountLifecyclePublisher, AccountLifecyclePublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
