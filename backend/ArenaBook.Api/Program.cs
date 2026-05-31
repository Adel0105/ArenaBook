using ArenaBook.Api.Endpoints;
using ArenaBook.Api.Infrastructure;
using ArenaBook.Application.Validation;
using ArenaBook.Infrastructure;
using ArenaBook.Infrastructure.Identity;
using ArenaBook.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' nije postavljen. Koristite ConnectionStrings__DefaultConnection ili ConnectionStrings:DefaultConnection.");
}

builder.Services.AddDbContext<ArenaBookDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ArenaBookDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddArenaBookAuthenticationStack(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ArenaBook API",
        Version = "v1",
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT u Authorization headeru: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
    await db.Database.MigrateAsync();

    await ArenaBookDbSeeder.SeedAsync(
        scope.ServiceProvider,
        app.Configuration,
        app.Environment.IsDevelopment());
}

app.UseExceptionHandler();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ArenaBook API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapArenaBookAuthEndpoints();
app.MapStripeWebhookEndpoints();
app.MapPayPalWebhookEndpoints();
app.MapCountryEndpoints();
app.MapCityEndpoints();
app.MapEquipmentTypeEndpoints();
app.MapSessionKindEndpoints();
app.MapSessionLifecycleStatusEndpoints();
app.MapPaymentProcessingStatusEndpoints();
app.MapPlatformSettingEntryEndpoints();
app.MapHallEndpoints();
app.MapHallPhotoEndpoints();
app.MapHallEquipmentEndpoints();
app.MapScheduledSessionEndpoints();
app.MapAdminDashboardEndpoints();
app.MapAdminUserEndpoints();
app.MapPlayerCoinEndpoints();
app.MapPlayerStripePaymentEndpoints();
app.MapPlayerPayPalPaymentEndpoints();
app.MapPlayerNotificationEndpoints();
app.MapPlayerMeEndpoints();
app.MapPlayerProfileEndpoints();
app.MapHallReviewEndpoints();
app.MapHallReactionEndpoints();
app.MapRecommendationEndpoints();
app.MapAdminCoinFinanceEndpoints();
app.MapAdminReportEndpoints();
app.MapDemoDataSeedEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "arena-book-api" }))
    .WithName("Health")
    .AllowAnonymous();

app.MapGet("/health/db", async (ArenaBookDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return ok
        ? Results.Ok(new { status = "Healthy", database = "connected" })
        : Results.StatusCode(503);
}).WithName("HealthDatabase")
.AllowAnonymous();

app.Run();

