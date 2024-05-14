using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Models.DataModels;
using Services.Extensions;
using Services.Repositories;

namespace PaymentFlow.Utils
{
    public static class ServiceSettings
    {
        private static bool HasConnectionString(this IConfiguration config, string name, out string? connectionString)
        {
            connectionString = config.GetConnectionString(name);
            return !string.IsNullOrWhiteSpace(connectionString);
        }

        public static void RegisterDependency(this WebApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddDbContext<DataContext>(options =>
            {
                if (builder.Configuration.HasConnectionString("SqlServer", out var sqlServerConnectionString))
                    options.UseSqlServer(sqlServerConnectionString);
                else if (builder.Configuration.HasConnectionString("MySQL", out var mySqlConnectionString))
                    options.UseMySql(mySqlConnectionString, new MySqlServerVersion(new Version(8, 0, 0)));

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.MultipleNavigationProperties));
                }
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddService();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        public static void AddDefaultCors(this WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .SetIsOriginAllowed(origin =>
                        {
                            var allowedHosts = builder.Configuration.GetValue<string>("AllowedHosts");
                            if (allowedHosts == "*")
                                return true;

                            var originHost = new Uri(origin).Host;
                            var allowedHostParts = allowedHosts.Split(';');

                            return allowedHostParts.Any(host => originHost.Equals(host, StringComparison.OrdinalIgnoreCase));
                        })
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }
    }
}
