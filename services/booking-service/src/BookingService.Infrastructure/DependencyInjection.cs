using BookingService.Application.Common.Interfaces;
using BookingService.Infrastructure.Common;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("BookingDb"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "booking")));

        services.AddScoped<IBookingDbContext>(sp => sp.GetRequiredService<BookingDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
