using BlazorAut.Data;
using BlazorAut.Services;
using BlazorAut.Middleware; // Adding namespace for Middleware
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using Microsoft.JSInterop;
using Radzen;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

string pfxFilePath = "";
//github
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    pfxFilePath = @"C:\crt\webaws_pam4_com.pfx";
}
else
{
    pfxFilePath = "/etc/ssl/certs/webaws_pam4_com.pfx";
}

// Kestrel for HTTPS
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps(pfxFilePath, "qaz123");
    });
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredLocalStorage();

// Add DbContext configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AppSettingsService
builder.Services.AddScoped<AppSettingsService>();

// Load app settings from the database
var serviceProvider = builder.Services.BuildServiceProvider();
var appSettingsService = serviceProvider.GetRequiredService<AppSettingsService>();
var appSettings = appSettingsService.GetAppSettingsAsync().Result;

var secretKey = appSettings["JwtSecretKey"];
var issuer = appSettings["JwtIssuer"];
var audience = appSettings["JwtAudience"];
var smtpServer = appSettings["SmtpServer"];
var smtpPort = int.Parse(appSettings["SmtpPort"]);
var smtpUser = appSettings["SmtpUser"];
var smtpPass = appSettings["SmtpPass"];
var key = Encoding.UTF8.GetBytes(secretKey);
var tokenExpirationDays = int.Parse(appSettings["TokenExpirationDays"]);

// Configure JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddScoped<JwtService>(provider => new JwtService(secretKey, issuer, audience));
builder.Services.AddScoped<IEmailService>(provider => new EmailService(smtpServer, smtpPort, smtpUser, smtpPass));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
{
    var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var jsRuntime = provider.GetRequiredService<IJSRuntime>();
    return new CustomAuthenticationStateProvider(serviceScopeFactory, context, httpContextAccessor, jsRuntime, secretKey, tokenExpirationDays);
});

builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped<DbServerInfoService>();
builder.Services.AddTransient<IEmailSender, GraphEmailSender>();
builder.Services.AddTransient<DbBackupService>();
builder.Services.AddSingleton<ViewOptionService>(); // Hide menu service
builder.Services.AddScoped<GetUserRolesService>(); // User roles
builder.Services.AddRadzenComponents();

// Add authorization policy for proxy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ProxyOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        // Optionally, add more requirements, e.g., roles
        // policy.RequireRole("Admin");
    });
});

// Configure CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseMiddleware<ClientInfoMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Adding custom middleware for proxying
app.UseProxyService();

// Map Blazor Hub and fallback
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
