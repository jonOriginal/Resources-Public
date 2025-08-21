using Backend.Api;
using Backend.Api.Services;
using Common;
using Config.Net;
using Dashboard;
using Dashboard.Components;
using Discord.OAuth;
using Microsoft.AspNetCore.Authentication.Cookies;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var config = new ConfigurationBuilder<IDashboardConfig>()
    .UseEnvironmentVariables()
    .UseCommandLineArgs()
    .Build();

builder.Services.AddConfiguration(config);
builder.AddRedisClient(Defaults.ServiceNames.Redis);

builder.Services.AddHttpClient<BackendClient>(
    client => client.BaseAddress = new Uri($"http://{Defaults.ServiceNames.Gateway}")
);

builder.Services.AddSingleton<ModService>();
builder.Services.AddSingleton<PostService>();

builder.Services.AddSingleton<DiscordAuth>();
builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = DiscordDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddDiscord(options =>
    {
        options.ClientId = config.DiscordAppId ??
                           throw new InvalidOperationException("DiscordClientId is not configured.");
        options.ClientSecret = config.DiscordAppSecret ??
                               throw new InvalidOperationException("DiscordClientSecret is not configured.");
        options.Scope.Add("guilds");
        options.Scope.Add("guilds.members.read");
        options.SaveTokens = true;
    });

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();