using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using RaceDay.Mvc.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
// Session state requires an IDistributedCache implementation.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Mvc.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Code attribution for cookie authentication:
// https://learn.microsoft.com/aspnet/core/security/authentication/cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Mvc.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<RaceDayApiClient>((serviceProvider, client) =>
{
    IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
    string apiBaseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5080/";
    client.BaseAddress = new Uri(apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(60);
})
// Disable the default shared CookieContainer. The current user's API session cookie
// is forwarded explicitly from that user's MVC session by RaceDayApiClient.
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Makes the MVC entry point discoverable by integration-test hosts if tests are added later.
public partial class Program { }
