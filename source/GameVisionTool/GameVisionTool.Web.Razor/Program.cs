using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SimpleInjector;
using System.Reflection;
using GameVisionTool.Web.Razor.CompositionRoot;
using ILogger = Serilog.ILogger;
using SimpleInjector.Lifestyles;

Log.Logger = CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var services = builder.Services;
// Add services to the container.
services.AddRazorPages();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// Open http://localhost:5132/swagger/ to browse the API.
services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
{
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "SOLID Services API" });
    });
}


var container = new Container();
container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

services.AddSimpleInjector(container, options =>
{
    options
        .AddAspNetCore()
        .AddPageModelActivation()
        .AddViewComponentActivation();
});

Bootstrapper.Bootstrap(container, builder.Configuration);

var app = builder.Build();

app.Services.UseSimpleInjector(container);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.UseSwagger();
app.UseSwaggerUI();

app.MapCommands(pattern: MessageMapping.FlatApi(new Commands(container), "/commands/{0}"), commandTypes: Bootstrapper.GetKnownCommandTypes());
app.MapQueries(pattern: MessageMapping.FlatApi(new Queries(container), "/queries/{0}"), queryTypes: Bootstrapper.GetKnownQueryTypes());
app.MapCommands(pattern: MessageMapping.FlatApi(new CommandsWithResults(container), "/commands/{0}"), commandTypes: Bootstrapper.GetKnownCommandResultTypes());

container.Verify();

app.Run();

return;

ILogger CreateLogger()
{
    var levelSwitch = new LoggingLevelSwitch
    {
#if DEBUG
        MinimumLevel = LogEventLevel.Debug,
#else
    MinimumLevel = LogEventLevel.Information,
#endif
    };

    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    var loggerConfig = new LoggerConfiguration();

    var logger = loggerConfig
        .MinimumLevel.ControlledBy(levelSwitch)
        .Enrich.WithProperty("Application", "GameVisionTool.Web")
        .ReadFrom.Configuration(config);

    return logger.CreateLogger();
}