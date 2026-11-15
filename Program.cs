using System.Text.Json.Serialization;
using ProjetoBtg.Api.Events;
using ProjetoBtg.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IContaRepository, InMemoryContaRepository>();
builder.Services.AddSingleton<IRegistroNotificacoes, RegistroNotificacoesEmMemoria>();
builder.Services.AddSingleton<IContaCriadaConsumer, ConsumidorPrevencaoFraude>();
builder.Services.AddSingleton<IContaCriadaConsumer, ConsumidorCartoes>();
builder.Services.AddSingleton<IContaCriadaEventPublisher, PublicadorContaCriada>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
