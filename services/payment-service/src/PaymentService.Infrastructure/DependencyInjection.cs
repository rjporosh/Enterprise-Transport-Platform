using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Infrastructure.Caching;
using PaymentService.Infrastructure.Common;
using PaymentService.Infrastructure.Jobs;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Observability;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Outbox;
using PaymentService.Infrastructure.Providers;
using Pomelo.EntityFrameworkCore.MySql;
using Quartz;
using System.Net.Http.Headers;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        services.AddDbContext<PaymentDbContext>(options =>
        {
            var provider = configuration["Database:Provider"] ?? "PostgreSQL";
            ApplyDatabaseProvider(options, provider, configuration.GetConnectionString("DefaultConnection") ?? string.Empty);
        });

        services.AddScoped<IPaymentDbContext>(sp => sp.GetRequiredService<PaymentDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddSingleton<IPaymentMetrics, PaymentMetrics>();

        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMQ"));
        services.Configure<BkashOptions>(configuration.GetSection("Bkash"));
        services.Configure<NagadOptions>(configuration.GetSection("Nagad"));
        services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
        services.Configure<Providers.QrCodeOptions>(configuration.GetSection(Providers.QrCodeOptions.SectionName));

        services.AddHttpClient("Bkash", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BkashOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient("Nagad", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NagadOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient("Stripe", (sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StripeOptions>>().Value.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddSingleton<DefaultPaymentProvider>();
        services.AddSingleton<BkashPaymentProvider>();
        services.AddSingleton<NagadPaymentProvider>();
        services.AddSingleton<StripePaymentProvider>();
        services.AddSingleton<QrPaymentProvider>();

        services.AddHostedService<OutboxProcessor>();

        if (!environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            services.AddQuartz(options =>
            {
                var reconciliationJobKey = new JobKey("payment-reconciliation-job");
                options.AddJob<PaymentReconciliationJob>(job => job.WithIdentity(reconciliationJobKey));
                options.AddTrigger(trigger => trigger
                    .WithIdentity("payment-reconciliation-trigger")
                    .ForJob(reconciliationJobKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));

                var webhookRetryJobKey = new JobKey("failed-webhook-retry-job");
                options.AddJob<FailedWebhookRetryJob>(job => job.WithIdentity(webhookRetryJobKey));
                options.AddTrigger(trigger => trigger
                    .WithIdentity("failed-webhook-retry-trigger")
                    .ForJob(webhookRetryJobKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever()));

                var verificationJobKey = new JobKey("agent-payment-method-verification-job");
                options.AddJob<AgentPaymentMethodVerificationJob>(job => job.WithIdentity(verificationJobKey));
                options.AddTrigger(trigger => trigger
                    .WithIdentity("agent-payment-method-verification-trigger")
                    .ForJob(verificationJobKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever()));
            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
        }

        return services;
    }

    private static void ApplyDatabaseProvider(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder options, string provider, string connectionString)
    {
        options = provider.ToLowerInvariant() switch
        {
            "postgresql" or "postgres" => options.UseNpgsql(connectionString),
            "sqlserver" or "mssql" => options.UseSqlServer(connectionString),
            "sqlite" => options.UseSqlite(connectionString),
            "mysql" => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported.")
        };
    }
}
