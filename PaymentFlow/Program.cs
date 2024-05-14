using Microsoft.AspNetCore.Authentication.Cookies;
using PaymentFlow.Middlewares;
using PaymentFlow.Utils;
using Serilog;
using Services.Extensions;
using System.Reflection;

try
{
    var builder = WebApplication.CreateBuilder(args);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    Log.Information("Starting web host");

    builder.Services.AddAutoMapper(Assembly.Load("Services"));

    builder.Services.AddConfigure();

    builder.Services.AddDistributedMemoryCache();

    // Add services to the container.
    var mvcBuilder = builder.Services
        .AddRazorPages();

    if (builder.Environment.IsDevelopment())
        mvcBuilder.AddRazorRuntimeCompilation();

    builder.Services.AddRouting(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
    });

    builder.Services.AddHttpClient();

    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
        // ToDo: §ó·s
        //options.ExcludedHosts.Add("example.com");
        //options.ExcludedHosts.Add("www.example.com");
    });

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "OrangeHouseAdmin";
            options.ExpireTimeSpan = TimeSpan.FromDays(1);
            options.LoginPath = "/admin/login";
            options.AccessDeniedPath = "/admin/error/401";
        });

    builder.RegisterDependency();

    // Serilog
    builder.Host.UseSerilog();

    var app = builder.Build();

    app.UseDbMigration();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseMiddleware<LogInformation>();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}