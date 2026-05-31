using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Abstractions.Notifications;
using ArenaBook.Application.Abstractions.Payments;
using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Abstractions.Sessions;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Options;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Authentication;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Services.Admin;
using ArenaBook.Infrastructure.Services.Coins;
using ArenaBook.Infrastructure.Services.Halls;
using ArenaBook.Infrastructure.Services.Messaging;
using ArenaBook.Infrastructure.Services.Notifications;
using ArenaBook.Infrastructure.Services.Payments;
using ArenaBook.Infrastructure.Services.Reference;
using ArenaBook.Infrastructure.Services.Sessions;
using ArenaBook.Application.Abstractions.Recommendations;
using ArenaBook.Infrastructure.Services;
using ArenaBook.Infrastructure.Services.Recommendations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArenaBook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddArenaBookAuthenticationStack(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwt = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || jwt.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey mora biti postavljen (npr. Jwt__SecretKey) i imati najmanje 32 znaka za HS256.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (string.IsNullOrEmpty(jti))
                            return;

                        var revocation = context.HttpContext.RequestServices
                            .GetRequiredService<IJwtTokenRevocationService>();
                        if (await revocation.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                            context.Fail("Token je opozvan.");
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AdministratorOnly, p =>
                p.RequireRole(ApplicationRoles.Administrator));
            options.AddPolicy(AuthPolicies.CatalogRead, p =>
                p.RequireRole(
                    ApplicationRoles.Administrator,
                    ApplicationRoles.Member,
                    ApplicationRoles.Organizer));
            options.AddPolicy(AuthPolicies.OrganizerArea, p =>
                p.RequireRole(ApplicationRoles.Organizer));
            options.AddPolicy(AuthPolicies.PlayerApp, p =>
                p.RequireRole(ApplicationRoles.Member, ApplicationRoles.Organizer));
        });

        services.AddSingleton<JwtTokenFactory>();
        services.AddScoped<IJwtTokenRevocationService, JwtTokenRevocationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerProfileService, PlayerProfileService>();
        services.AddScoped<ISessionOrganizerRoleService, SessionOrganizerRoleService>();
        services.AddScoped<ICountryService, CountryService>();
        services.AddScoped<ICityService, CityService>();
        services.AddScoped<IEquipmentTypeService, EquipmentTypeService>();
        services.AddScoped<ISessionKindService, SessionKindService>();
        services.AddScoped<ISessionLifecycleStatusService, SessionLifecycleStatusService>();
        services.AddScoped<IPaymentProcessingStatusService, PaymentProcessingStatusService>();
        services.AddScoped<IPlatformSettingEntryService, PlatformSettingEntryService>();
        services.AddScoped<IHallService, HallService>();
        services.AddScoped<IHallPhotoService, HallPhotoService>();
        services.AddScoped<IHallEquipmentService, HallEquipmentService>();
        services.AddScoped<IHallReviewService, HallReviewService>();
        services.AddScoped<IHallReactionService, HallReactionService>();
        services.AddScoped<IRecommendationService, CollaborativeFilteringRecommendationService>();
        services.AddScoped<IScheduledSessionService, ScheduledSessionService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IDemoDataSeedService, DemoDataSeedService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IPlayerCoinService, PlayerCoinService>();
        services.AddScoped<ICoinPurchaseFinalizer, CoinPurchaseFinalizer>();
        services.AddScoped<IAdminCoinFinanceService, AdminCoinFinanceService>();
        services.AddScoped<IAdminReportService, AdminReportService>();

        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.Configure<PayPalOptions>(configuration.GetSection(PayPalOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IRabbitMqEventPublisher, RabbitMqEventPublisher>();
        services.AddHttpClient(
            "paypal",
            (sp, client) =>
            {
                var o = sp.GetRequiredService<IOptions<PayPalOptions>>().Value;
                var url = (string.IsNullOrWhiteSpace(o.BaseApiUrl) ? "https://api-m.sandbox.paypal.com" : o.BaseApiUrl).TrimEnd('/');
                client.BaseAddress = new Uri(url + "/");
                client.Timeout = TimeSpan.FromSeconds(120);
            });
        services.AddScoped<IStripeCoinSandboxService, StripeCoinSandboxService>();
        services.AddScoped<IPayPalCoinSandboxService, PayPalCoinSandboxService>();
        services.AddScoped<IPlayerNotificationService, PlayerNotificationService>();

        return services;
    }
}

