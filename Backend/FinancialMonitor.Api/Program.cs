using System.Text.Json.Serialization;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FinancialMonitor.Api.Data;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON configuration
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                namingPolicy: null,
                allowIntegerValues: false
            )
        );
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnectionString =
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// Health Checks כולל בדיקת זמינות Redis עבור Readiness
builder.Services.AddHealthChecks()
    .AddRedis(
        redisConnectionString,
        name: "redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" }
    );

// SignalR + Redis Backplane
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("FinancialMonitor");
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                namingPolicy: null,
                allowIntegerValues: false
            )
        );
    });

// Application services
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing."
    );

builder.Services.AddDbContext<FinancialMonitorDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<
    ITransactionRepository,
    PostgresTransactionRepository
>();

builder.Services.AddSingleton<
    ITransactionNotifier,
    SignalRTransactionNotifier
>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinancialMonitor.Api v1");
        c.RoutePrefix = "swagger";
        c.ConfigObject.DeepLinking = true;
    });
}

app.UseHttpsRedirection();

app.UseCors("ReactClient");

app.MapControllers();
app.MapHub<TransactionHub>("/hubs/transactions");

// Health Checks endpoints נפרדים לענן / Kubernetes
// 1. Liveness - בודק רק שהתהליך בחיים
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// 2. Readiness - בודק את כל התלויות הקריטיות (כולל Redis)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program { }