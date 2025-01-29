using BlazorAut.Data;
using BlazorAut.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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


//  Kestrel for HTTPS
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
//builder.Services.AddSingleton<WeatherForecastService>();
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
var gptKey = appSettings["GptKey"];
var issuer = appSettings["JwtIssuer"];
var audience = appSettings["JwtAudience"];
var smtpServer = appSettings["SmtpServer"];
var smtpPort = int.Parse(appSettings["SmtpPort"]);
var smtpUser = appSettings["SmtpUser"];
var smtpPass = appSettings["SmtpPass"];
var key = Encoding.UTF8.GetBytes(secretKey);
var tokenExpirationDays = int.Parse(appSettings["TokenExpirationDays"]);
var AZURE_OPENAI_ENDPOINT = appSettings["AZURE_OPENAI_ENDPOINT"];
var AZURE_OPENAI_API_KEY = appSettings["AZURE_OPENAI_API_KEY"];

builder.Services.AddHttpClient();

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


//builder.Services.AddScoped<AuthenticationStateProvider>(provider => new CustomAuthenticationStateProvider(provider.GetRequiredService<ApplicationDbContext>(), secretKey));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
{
    var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var jsRuntime = provider.GetRequiredService<IJSRuntime>();
    return new CustomAuthenticationStateProvider(serviceScopeFactory, context, httpContextAccessor, jsRuntime, secretKey, tokenExpirationDays);
});

builder.Services.AddScoped<IChatService>(provider => new ChatService(
    provider.GetRequiredService<HttpClient>(),
    gptKey
));

builder.Services.AddScoped<IChatAzureService>(provider => new ChatAzureService(
    provider.GetRequiredService<HttpClient>(),
    AZURE_OPENAI_ENDPOINT,
    AZURE_OPENAI_API_KEY
));

builder.Services.AddAuthorizationCore();
builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazoredSessionStorage(); //
builder.Services.AddScoped<DbServerInfoService>();
builder.Services.AddTransient<IEmailSender, GraphEmailSender>();
builder.Services.AddTransient<DbBackupService>();
builder.Services.AddSingleton<ViewOptionService>(); //hide menue service
builder.Services.AddScoped<GetUserRolesService>(); //user roles
builder.Services.AddRadzenComponents(); //Radzen components

var app = builder.Build();

//app.MapGet("/", async (IEmailSender emailSender) =>
//{
//    await emailSender.SendEmailAsync("leshkov@servilon.com", "Test Subject", "Test Message");
//    return "Email sent successfully";
//});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseMiddleware<ClientInfoMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
