using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;
using System;
using UserServiceAPI.Models;
using UserServiceAPI.Services;
using UserServiceAPI.Interfaces;
using UserServiceAPI.Data;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    // Vault Setup: Retrieve secrets
    var vaultService = new VaultService(configuration);

    string mySecret = await vaultService.GetSecretAsync("secrets", "SecretKey") ?? "????";
    string myIssuer = await vaultService.GetSecretAsync("secrets", "IssuerKey") ?? "????";
    string myConnectionString = await vaultService.GetSecretAsync("secrets", "MongoConnectionString") ?? "????";

    // Add retrieved secrets to the configuration
    configuration["SecretKey"] = mySecret;
    configuration["IssuerKey"] = myIssuer;
    configuration["MongoConnectionString"] = myConnectionString;

    logger.Info($"Secret: {mySecret}");
    logger.Info($"Issuer: {myIssuer}");
    logger.Info($"MongoConnectionString: {myConnectionString}");

    // Authentication & Authorization Configuration
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = myIssuer,
            ValidAudience = "http://localhost",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(mySecret)),
            ClockSkew = TimeSpan.Zero // Disable default clock skew
        };

        // Handle expired tokens
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                    logger.Error($"Token expired: {context.Exception.Message}");
                }
                return Task.CompletedTask;
            }
        };
    });

    // Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("UserRolePolicy", policy => policy.RequireRole("1"));
        options.AddPolicy("AdminRolePolicy", policy => policy.RequireRole("2"));
    });

    // CORS Policy Configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowOrigin", policyBuilder =>
        {
            policyBuilder.AllowAnyHeader()
                         .AllowAnyMethod();
        });
    });

    // Register Swagger for API documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register Controllers
    builder.Services.AddControllers();

    // Dependency Injection Configuration
    builder.Services.AddTransient<VaultService>(); // Vault service
    builder.Services.AddSingleton<MongoDBContext>(); // MongoDB context
    builder.Services.AddSingleton<IUserInterface, UserMongoDBService>(); // User service

    // Configure Logging
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var app = builder.Build();

    // Middleware Pipeline Configuration
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
    app.UseCors("AllowOrigin"); // Enable CORS
    app.UseAuthentication(); // Enable Authentication
    app.UseAuthorization(); // Enable Authorization

    // Seed database during startup
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MongoDBContext>();
        var contextLogger = scope.ServiceProvider.GetRequiredService<ILogger<MongoDBContext>>(); // Henter en ASP.NET Core logger, fordi MongoDB ikke er kompatibel med NLog åbenbart? ChatGPT siger det her
        await context.SeedDataAsync(contextLogger); // Brug loggeren til seeding
    }

    app.MapControllers(); // Map Controllers
    app.Run(); // Run the application
}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
