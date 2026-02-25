using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("FunctionApp", Environment.GetEnvironmentVariable("FUNCTION_APP_NAME"))
    .WriteTo.Console(new RenderedCompactJsonFormatter()).CreateLogger();

var builder = FunctionsApplication.CreateBuilder(args);

// Charge appsettings.json + variables d'environnement
//var host = new HostBuilder()
//    .ConfigureFunctionsWorkerDefaults()
//    .ConfigureAppConfiguration(config => {
//        config.AddEnvironmentVariables(); // Force le chargement
//    })
//    .Build();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

//builder.Services.AddTransient<IInvoiceProcessService, InvoiceProcessService>();

builder.Services.AddLogging(lb =>
{
    lb.ClearProviders();
    lb.AddSerilog();
});

builder.Build().Run();
