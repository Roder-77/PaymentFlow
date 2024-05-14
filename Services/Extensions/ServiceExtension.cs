using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.DataModels;
using Scrutor;
using Services.HostedServices;
using Services.Repositories;
using static Services.Interface.IMailService;

namespace Services.Extensions
{
    public static class ServiceExtension
    {
        public static void AddService(this IServiceCollection services)
        {
            var baseServiceType = typeof(BaseService<>);

            services.Scan(scan =>
                scan.FromAssembliesOf(baseServiceType)
                    .AddClasses(classes => classes.AssignableTo(baseServiceType))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelf()
                    .WithScopedLifetime()
            );

            services.AddSingleton<JwtService>();
            services.AddSingleton<CallApiService>();
            services.AddSingleton<CommonService>();
        }

        public static void AddRepository(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
        {
            services.AddDbContext<DataContext>(optionsAction);

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        }

        public static void AddHostedService(this IServiceCollection services)
            => services.AddHostedService<TestBackGroundService>();

        public static void AddMailService(this IServiceCollection services)
        {
            services.AddOptions<MailSettings>()
                .Configure<IConfiguration>((settings, config) => config.GetSection("MailSettings").Bind(settings));

            services.AddScoped<MailService>();
            services.AddTransient<MailServiceResolver>(serviceProvider => type =>
            {
                return type switch
                {
                    MailServiceType.Normal => serviceProvider.GetRequiredService<MailService>()!,
                    _ => throw new NotSupportedException()
                };
            });
        }

        public static void AddHangfire(this IServiceCollection services, string connectionString)
        {
            services.AddHangfire(config =>
            {
                var storageOptions = new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                };
                var storage = new SqlServerStorage(connectionString, storageOptions);

                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseStorage(storage);
            });

            services.AddHangfireServer(options => options.WorkerCount = 10);
        }

        public static void AddConfigure(this IServiceCollection services)
        {
            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });

            services.Configure<KestrelServerOptions>(options => options.Limits.MaxRequestBodySize = int.MaxValue);

            services.AddOptions<Appsettings>()
                .Configure<IConfiguration>((settings, config) => config.GetSection("ApprovalSettings").Bind(settings));

            services.AddOptions<EcPaySettings>()
                .Configure<IConfiguration>((ecPay, config) => config.GetSection("EcPaySettings").Bind(ecPay));

            services.AddOptions<NewebPaySettings>()
                .Configure<IConfiguration>((newebPay, config) => config.GetSection("NewebPaySettings").Bind(newebPay));
        }
    }
}
