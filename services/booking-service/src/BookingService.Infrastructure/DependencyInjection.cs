using BookingService.Application.Common.Interfaces;
using BookingService.Infrastructure.Caching;
using BookingService.Infrastructure.Common;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Observability;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

        // Single shared multiplexer for the app's lifetime, per StackExchange.Redis
        // guidance — cheap to reuse, expensive to reconnect per request.
        // AbortOnConnectFail=false is what actually makes RedisCacheService's
        // "fail open" behavior true end-to-end: without it, a Redis outage at
        // startup would throw here and take the whole API down with it.
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectTimeout = 3000;
            return ConnectionMultiplexer.Connect(configOptions);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IBookingMetrics, BookingMetrics>();

        return services;
    }
}
