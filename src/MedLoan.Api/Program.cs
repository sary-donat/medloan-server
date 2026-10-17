using System.Text;
using System.Text.Json.Serialization;
using MedLoan.Api.Auth;
using MedLoan.Api.Middleware;
using MedLoan.Application;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Enums;
using MedLoan.Infrastructure;
using MedLoan.Infrastructure.Auth;
using MedLoan.Infrastructure.Persistence;
using MedLoan.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
    builder.Services.AddProblemDetails();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    // Settings come from IOptions<JwtOptions>: one source, validated on start (9.3).
    // The section is not read directly with Get<JwtOptions>()!, which may return null.
    builder.Services
        .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;
            options.MapInboundClaims = false;            // keeps "sub" and "role" as they are, without mapping to long names
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],   // rejects tokens signed with another algorithm
                NameClaimType = "name",
                RoleClaimType = "role",
                ClockSkew = TimeSpan.FromSeconds(30)                 // the default of 5 minutes is too long
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    // Only the failure type: the token itself is never logged.
                    ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                       .LogWarning("JWT authentication failed: {Reason}", ctx.Exception.GetType().Name);
                    return Task.CompletedTask;
                }
            };
        });

    // Secure by default: every endpoint requires a logged-in user unless it is marked [AllowAnonymous].
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Policies.AdminOnly, p => p.RequireRole(nameof(UserRole.Admin)))
        .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    // Without this, UseForwardedHeaders does nothing and the Render proxy is not trusted.
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(o => o.AddPolicy("client", p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    // The system starts with no manual DB step, locally and on Render (13).
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(CancellationToken.None);
        logger.Info("Database migrations applied");

        var seeded = await DbInitializer.SeedAsync(
            db,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            CancellationToken.None);
        if (seeded > 0)
            logger.Info("Seeded {SeededCount} demo members", seeded);
    }

    app.UseForwardedHeaders();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseStatusCodePages();
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "MedLoan API"));
    app.UseCors("client");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)   // dotnet ef stops the host with this exception — not an error
{
    logger.Error(ex, "Application stopped because of an exception during startup");
    throw;
}
finally
{
    LogManager.Shutdown();
}
