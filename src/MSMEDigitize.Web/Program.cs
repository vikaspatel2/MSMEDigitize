using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Application.Services;
using MSMEDigitize.Infrastructure;
using MSMEDigitize.Infrastructure.Data;
using MSMEDigitize.Web.Filters;
using MSMEDigitize.Web.Middlewares;
using Serilog;
using System.Text;
using MSMEDigitize.Web;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    //.Enrich.WithMachineName()
    .WriteTo.Console()
    //.WriteTo.MSSqlServer(
    //    connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
    //    tableName: "Logs",
    //    autoCreateSqlTable: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();
// Core Services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
//builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// MVC
builder.Services.AddControllersWithViews(options => {
    options.Filters.Add<TenantAuthorizationFilter>();
    options.Filters.Add<AuditLogFilter>();
});

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// JWT Authentication (for API)
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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

// Rate Limiting
builder.Services.AddMemoryCache();

// SignalR for real-time notifications
builder.Services.AddSignalR();

// Response compression
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

// Seed & recurring jobs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}
//DependencyInjection.RegisterRecurringJobs();

app.Run();
