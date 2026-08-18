using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Abstractions.Security;
using BookManager.Application.Services;
using BookManager.Infrastructure.DataAccess;
using BookManager.Infrastructure.DataAccess.Repositories;
using BookManager.Infrastructure.DataAccess.UnitOfWork;
using BookManager.Infrastructure.Security;
using BookManager.Presentation.BackgroundServices;
using BookManager.Presentation.Middleware;
using BookManager.Presentation.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.Configure<ApiBehaviorOptions>(
    options =>
    {
        options.InvalidModelStateResponseFactory =
            context =>
            {
                var details =
                    new ValidationProblemDetails(
                        context.ModelState)
                    {
                        Status =
                            StatusCodes.Status400BadRequest,
                        Title = "Validation error",
                        Detail =
                            "One or more validation errors occurred."
                    };

                return new BadRequestObjectResult(details);
            };
    });

builder.Services.AddEndpointsApiExplorer();

var jwtOptions =
    builder.Configuration
        .GetSection("Jwt")
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) ||
    jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT secret must contain at least 32 characters.");
}

builder.Services.AddSingleton(jwtOptions);

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Secret)),

                RoleClaimType =
                    System.Security.Claims
                        .ClaimTypes.Role,

                NameClaimType =
                    System.Security.Claims
                        .ClaimTypes.Name,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(
            AppContext.BaseDirectory,
            xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header
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
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseNpgsql(connectionString));

builder.Services.AddScoped<
    IEventRepository,
    EventRepository>();

builder.Services.AddScoped<
    IBookingRepository,
    BookingRepository>();

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<
    IUnitOfWork,
    UnitOfWork>();

builder.Services.AddSingleton<
    IPasswordHasher,
    PasswordHasher>();

builder.Services.AddSingleton<
    IJwtTokenGenerator,
    JwtTokenGenerator>();

builder.Services.AddScoped<
    IEventService,
    EventService>();

builder.Services.AddScoped<
    IBookingService,
    BookingService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.Configure<BookingProcessingOptions>(
    builder.Configuration.GetSection(
        "BookingProcessing"));

builder.Services.AddHostedService<
    BookingBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    database.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
