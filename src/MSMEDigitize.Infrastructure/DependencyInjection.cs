using Microsoft.AspNetCore.Identity;
using MSMEDigitize.Core.Entities;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.BackgroundJobs;
using MSMEDigitize.Infrastructure.Caching;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Infrastructure.ExternalServices;
using MSMEDigitize.Infrastructure.Messaging;
using MSMEDigitize.Infrastructure.Messaging.RabbitMQ;
using RabbitMQ.Client;
using MSMEDigitize.Infrastructure.Services;
using MSMEDigitize.Infrastructure.Security;
using SmsService = MSMEDigitize.Infrastructure.ExternalServices.SmsService;

namespace MSMEDigitize.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── EF Core: Business DbContext ──────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    sql.CommandTimeout(120);
                    sql.MigrationsAssembly("MSMEDigitize.Infrastructure");
                }));


        // ── ASP.NET Identity ─────────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ── Redis Cache ───────────────────────────────────────────────────────
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = config.GetConnectionString("Redis") ?? "localhost:6379";
            opts.InstanceName = "MSMEDigitize:";
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // ── Repository / Unit of Work ─────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── External Communication Services ───────────────────────────────────
        services.AddScoped<IEmailService, SmtpEmailService>();  // No external packages needed; configure via appsettings Smtp section
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IPaymentGatewayService, RazorpayPaymentService>();
        // ── Message Bus (RabbitMQ) ─────────────────────────────────────────────
        var rabbitHost = config.GetConnectionString("RabbitMQ") ?? config["RabbitMQ:Host"];
        if (!string.IsNullOrEmpty(rabbitHost))
        {
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = config["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                    UserName = config["RabbitMQ:Username"] ?? "guest",
                    Password = config["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/"
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            services.AddScoped<IMessageBus, RabbitMQMessageBus>();
        }
        else
        {
            // No RabbitMQ configured — use in-process no-op bus
            services.AddScoped<IMessageBus, NullMessageBus>();
        }

        // ── HTTP Clients ──────────────────────────────────────────────────────
        services.AddHttpClient("GSTPortal", c =>
        {
            c.BaseAddress = new Uri(config["GSTPortal:BaseUrl"] ?? "https://api.gst.gov.in/");
            c.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient("WhatsApp", c =>
        {
            c.BaseAddress = new Uri(config["WhatsApp:BaseUrl"] ?? "https://api.whatsapp.com/");
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient("AccountAggregator", c =>
        {
            c.BaseAddress = new Uri(config["AccountAggregator:BaseUrl"] ?? "https://api.setu.co/");
            c.Timeout = TimeSpan.FromSeconds(45);
        });

        // ── Storage Service ───────────────────────────────────────────────────
        services.AddScoped<IStorageService, LocalStorageService>();

        // ── Core Business Services ────────────────────────────────────────────
        services.AddScoped<INotificationService, NotificationServiceImpl>();
        services.AddScoped<IPDFService, PDFServiceImpl>();
        //services.AddScoped<IPdfService, PDFServiceImpl>();
        services.AddScoped<IGSTService, GSTServiceImpl>();
        services.AddScoped<IAIService, AIServiceImpl>();
        services.AddScoped<IBankingService, BankingServiceImpl>();
        //services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAuthService, AuthService>();
        //services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // ── Hangfire ──────────────────────────────────────────────────────────
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(config.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    PrepareSchemaIfNecessary = true   // auto-create HangFire.* tables
                }));
        services.AddHangfireServer(opts =>
        {
            opts.WorkerCount = Environment.ProcessorCount * 2;
            opts.Queues = new[] { "critical", "default", "low" };
        });

        // ── Background Jobs ───────────────────────────────────────────────────
        services.AddScoped<InvoiceReminderJob>();
        services.AddScoped<LowStockAlertJob>();
        services.AddScoped<SubscriptionRenewalJob>();
        services.AddScoped<GSTReminderJob>();

        return services;
    }

    //public static void RegisterRecurringJobs()
    //{
    //    RecurringJob.AddOrUpdate<InvoiceReminderJob>("invoice-overdue-reminders",
    //        j => j.SendOverdueRemindersAsync(), Cron.Daily(9), queue: "default");
    //    RecurringJob.AddOrUpdate<LowStockAlertJob>("low-stock-check",
    //        j => j.CheckLowStockAsync(), Cron.Daily(8), queue: "default");
    //    RecurringJob.AddOrUpdate<SubscriptionRenewalJob>("subscription-renewal",
    //        j => j.ProcessExpiringSubscriptionsAsync(), Cron.Daily(7), queue: "critical");
    //    RecurringJob.AddOrUpdate<GSTReminderJob>("gst-reminders",
    //        j => j.SendGSTFilingRemindersAsync(), "0 9 1,7,11,18,20 * *", queue: "default");
    //}
}