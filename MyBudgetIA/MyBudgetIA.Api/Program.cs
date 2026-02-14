using Azure.Identity;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.Extensions.Options;
using MyBudgetIA.Api.Middlewares;
using MyBudgetIA.Application;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Extensions;
using Serilog;

// Minitial and temp logger used before full Serilog configuration is loaded
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MyBudgetIA API in {Environment}",
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");

    var builder = WebApplication.CreateBuilder(args);

    // In containers, listen on HTTP only; ACA terminates TLS at the ingress.
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });

    // Real Serilog configuration
    builder.Host.UseSerilog((context, services, configuration) =>
       configuration
           .ReadFrom.Configuration(context.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Application", "MyBudgetIA.API")
           .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    builder.Services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();

    // Add services to the container.
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Application started successfully");
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}

