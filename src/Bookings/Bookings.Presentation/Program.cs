using Bookings.Presentation;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Bookings.Application;
using Bookings.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

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

builder.Services.AddDbContext<BookingsDbContext>(
    options =>
        options.UseNpgsql(
            connectionString));

builder.Services.AddScoped<
    IBookingRepository,
    BookingRepository>();

builder.Services.AddScoped<
    BookingService>();

var kafka =
    builder.Configuration
        .GetSection("Kafka")
        .Get<KafkaOptions>()
    ?? throw new InvalidOperationException(
        "Kafka configuration is missing.");

builder.Services.AddSingleton(kafka);

var processing =
    builder.Configuration
        .GetSection("BookingProcessing")
        .Get<BookingProcessingOptions>()
    ?? new BookingProcessingOptions();

builder.Services.AddSingleton(processing);

builder.Services.AddSingleton<
    IBookingEventPublisher,
    KafkaBookingEventPublisher>();

builder.Services.AddHostedService<
    BookingConfirmationWorker>();

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
            .GetRequiredService<
                BookingsDbContext>();

    await db.Database.MigrateAsync();
}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "ok",
            service = "bookings"
        }));

app.Run();
