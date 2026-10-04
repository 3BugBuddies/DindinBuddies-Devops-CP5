using System.Text.Json.Serialization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using DindinBuddies.Api.Erros;
using DindinBuddies.Api.Inicializacao;
using DindinBuddies.Application;
using DindinBuddies.Infrastructure;

const string PoliticaCorsFront = "Front";

var builder = WebApplication.CreateBuilder(args);

// Connection string vem de variável de ambiente (ConnectionStrings__DindinBuddies); nunca do repositório.
var connectionString = builder.Configuration.GetConnectionString("DindinBuddies");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Connection string 'DindinBuddies' não configurada. Defina a variável de ambiente ConnectionStrings__DindinBuddies.");

// Telemetria só liga quando o App Insights está configurado (no Web App da Azure).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();
builder.Services.AddOpenApi();

var origemFront = builder.Configuration["Cors:OrigemFront"];
builder.Services.AddCors(o => o.AddPolicy(PoliticaCorsFront, politica =>
{
    if (!string.IsNullOrWhiteSpace(origemFront))
        politica.WithOrigins(origemFront.TrimEnd('/')).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHostedService<AplicarMigrationsHostedService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger fica ativo também no Web App, para o avaliador testar.
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "DindinBuddies API");
    o.DocumentTitle = "DindinBuddies API";
});

app.UseCors(PoliticaCorsFront);
app.MapControllers();

// A raiz leva direto ao Swagger.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
