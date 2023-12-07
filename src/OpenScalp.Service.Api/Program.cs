using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using OpenScalp.QuikSharp;
using OpenScalp.Service.Api;
using OpenScalp.TradingTerminal.Abstractions;
using OpenScalp.TradingTerminal.Quik;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext());


var services = builder.Services;
var configuration = builder.Configuration;

services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.CustomSchemaIds(x => x.FullName);
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OpenScalp.Service.Api",
        Description = "OpenScalp Service Api"
    });
});

if (builder.Environment.IsDevelopment())
{
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin();
        });
    });
}


services.AddHealthChecks().ForwardToPrometheus();

services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });

services.Configure<QuikTradingTerminalConnectionOptions>(configuration.GetSection("QuikTradingTerminalConnection"));
services.AddSingleton<ITradingTerminalConnection, QuikTradingTerminalConnection>();
services.AddHostedService<QuikOrderBookPrototypeHostService>();

var app = builder.Build();

app.UseRouting();
app.UseCors();
app.UseHttpMetrics();
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    cfg => cfg.UseHttpLogging()
);

app.MapControllers();

app.UseSwagger(options => { options.RouteTemplate = "swagger/{documentName}/swagger.json"; })
    .UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("v1/swagger.json", "OpenScalp api v1");
    });

app.MapHealthChecks("/health");
app.MapMetrics();
app.MapHub<TradingHub>("/tradingHub");

app.Run();