using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Infrastructure.Common;
using TicketingService.Infrastructure.Messaging;
using TicketingService.Infrastructure.Pdf;
using TicketingService.Infrastructure.Persistence;
using TicketingService.Infrastructure.Persistence.Outbox;

namespace TicketingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);

        services.Configure<TicketingSettings>(configuration.GetSection("Ticketing"));

        services.AddScoped<ITicketingDbContext>(sp => sp.GetRequiredService<TicketingDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ITicketPdfRenderer, QuestPdfTicketRenderer>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<BookingConfirmedConsumer>();

        return services;
    }

    /// <summary><c>Database:Provider</c> = Postgres (default) | SqlServer | MySql. See docs/programmers-guide/database-provider-factory.md.</summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "Postgres").Trim().ToLowerInvariant();
        var cs = configuration.GetConnectionString("TicketingDb")
            ?? throw new InvalidOperationException("ConnectionStrings:TicketingDb is not configured.");

        services.AddDbContext<TicketingDbContext>(options =>
        {
            switch (provider)
            {
                case "postgres" or "postgresql" or "npgsql":
                    options.UseNpgsql(cs, o => o.MigrationsAssembly("TicketingService.Infrastructure")
                        .MigrationsHistoryTable("__ef_migrations_history", "ticketing"));
                    break;
                case "sqlserver" or "mssql":
                    options.UseSqlServer(cs, o => o.MigrationsAssembly("TicketingService.Infrastructure")
                        .MigrationsHistoryTable("__ef_migrations_history", "ticketing"));
                    break;
                case "mysql":
                    options.UseMySql(cs, ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql),
                        o => o.MigrationsAssembly("TicketingService.Infrastructure"));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Database:Provider '{provider}'. Supported: Postgres, SqlServer, MySql.");
            }
        });
    }
}
