using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.BackgroundServices;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Middleware;
using MyWebApiProject.Options;
using MyWebApiProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails =
            new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = "One or more validation errors occurred."
            };

        return new BadRequestObjectResult(problemDetails);
    };
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddSingleton<
    IEventStore,
    InMemoryEventStore>();

builder.Services.AddSingleton<
    IEventService,
    EventService>();

builder.Services.AddSingleton<
    IBookingStore,
    InMemoryBookingStore>();

builder.Services.AddSingleton<
    IBookingService,
    BookingService>();

builder.Services.Configure<BookingProcessingOptions>(
    builder.Configuration.GetSection("BookingProcessing"));

builder.Services.AddHostedService<
    BookingBackgroundService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
