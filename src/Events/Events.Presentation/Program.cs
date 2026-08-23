using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using Events.Presentation;
using System.Security.Claims;
using System.Text;
using Events.Application;
using Events.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["Service:Name"]
    ?? "events-service";

var otlpEndpoint =
    builder.Configuration["Otlp:Endpoint"]
    ?? "http://localhost:4317";

builder.Host.UseSerilog(
    (context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(
                context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                new CompactJsonFormatter());
    });

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(
            serviceName: serviceName))
    .WithTracing(tracing =>
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint =
                    new Uri(otlpEndpoint);
            }))
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is missing.");

builder.Services.AddDbContext<EventsDbContext>(
    options =>
        options.UseNpgsql(connectionString));

builder.Services.AddScoped<
    IEventRepository,
    EventRepository>();

var eventCacheOptions =
    builder.Configuration
        .GetSection("Redis")
        .Get<EventCacheOptions>()
    ?? new EventCacheOptions();

builder.Services.AddSingleton(
    eventCacheOptions);

var redisConnectionString =
    builder.Configuration[
        "Redis:ConnectionString"]
    ?? "localhost:6379";

var redisConfiguration =
    ConfigurationOptions.Parse(
        redisConnectionString);

redisConfiguration.AbortOnConnectFail = false;
redisConfiguration.ConnectRetry = 1;
redisConfiguration.ConnectTimeout = 1000;
redisConfiguration.SyncTimeout = 1000;

builder.Services.AddSingleton<
    IConnectionMultiplexer>(
        _ =>
            ConnectionMultiplexer.Connect(
                redisConfiguration));

builder.Services.AddSingleton<
    ICacheService,
    RedisCacheService>();

builder.Services.AddScoped<EventService>();

var kafka =
    builder.Configuration
        .GetSection("Kafka")
        .Get<KafkaOptions>()
    ?? throw new InvalidOperationException(
        "Kafka configuration is missing.");

builder.Services.AddSingleton(kafka);

builder.Services.AddHostedService<
    KafkaTopicInitializer>();

builder.Services.AddHostedService<
    BookingConfirmedConsumer>();

var jwtSecret =
    builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret is missing.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSecret)),

                RoleClaimType =
                    ClaimTypes.Role,

                NameClaimType =
                    ClaimTypes.Name
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await using (var scope =
    app.Services.CreateAsyncScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<EventsDbContext>();

    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet(
    "/health",
    () => Results.Ok(
        new { status = "ok", service = "events" }));

app.MapPrometheusScrapingEndpoint();

app.Run();
