using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Web.Filters;
using MSMEDigitize.Web.Middlewares;
using Serilog;
using System.Text;
using MSMEDigitize.Web;
using MSMEDigitize.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure (includes Hangfire registration internally)
builder.Services.AddInfrastructure(builder.Configuration);

// Core Services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// MVC
builder.Services.AddControllersWithViews(options => {
    options.Filters.Add<TenantAuthorizationFilter>();
    options.Filters.Add<AuditLogFilter>();
});

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => {
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options => {
    options.AddPolicy("RequireOwner", p => p.RequireRole("Owner", "SuperAdmin"));
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Owner", "Admin", "SuperAdmin"));
    options.AddPolicy("RequireAccountant", p => p.RequireRole("Owner", "Admin", "Accountant", "SuperAdmin"));
    options.AddPolicy("RequireManager", p => p.RequireRole("Owner", "Admin", "Manager", "SuperAdmin"));
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));
});

builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression();

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard (SuperAdmin only)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/hubs/notifications");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// Register Hangfire recurring jobs AFTER app is built and Hangfire schema is ready
// Wrapped in try/catch so a Hangfire DB issue won't crash the whole app
//try
//{
//    DependencyInjection.RegisterRecurringJobs();
//}
//catch (Exception ex)
//{
//    Log.Warning(ex, "Could not register Hangfire recurring jobs - Hangfire schema may not be ready yet. Jobs will be registered on next startup.");
//}

app.Run();