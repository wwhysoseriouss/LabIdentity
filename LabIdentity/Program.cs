using LabData.Data;
using LabData.Repositories;
using LabData.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using LabIdentity.Configuration;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using LabIdentity.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Insert(0, new JsonConfigurationSource
{
    Path = "sharedsettings.json",
    FileProvider = builder.Environment.ContentRootFileProvider,
    Optional = false,
    ReloadOnChange = true
});

var settings = builder.Configuration.Get<LabSettings>()
    ?? throw new InvalidOperationException("Не вдалося прочитати налаштування.");

var connectionString = settings.ConnectionStrings.DefaultConnection;

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не задано ConnectionStrings:DefaultConnection.");
}

builder.Services.AddSingleton(settings);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("VerifiedClient", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsVerifiedClient", "true");
    });
    options.AddPolicy("EditOwnPollDraft", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PollDraftAuthorRequirement());
    });
    options.AddPolicy("PremiumAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new MinimumWorkingHoursRequirement(100));
    });
    options.AddPolicy("ForumAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ForumAccessRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, PollDraftAuthorHandler>();

builder.Services.AddSingleton<IAuthorizationHandler, MinimumWorkingHoursHandler>();

builder.Services.AddSingleton<IAuthorizationHandler, ForumAccessHandler>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ConfirmEmail");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/LoginWith2fa");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/LoginWithRecoveryCode");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResetPassword");
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = userId is null ? $"guest:{ipAddress}" : $"user:{userId}";
        var permitLimit = userId is null ? 10 : 30;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();