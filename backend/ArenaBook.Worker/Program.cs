using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Options;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Services.Messaging;
using ArenaBook.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' nije postavljen. Koristite ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContextFactory<ArenaBookDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "RabbitMQ host is required")
    .ValidateOnStart();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

