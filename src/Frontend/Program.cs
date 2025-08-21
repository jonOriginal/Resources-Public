using Backend.Api;
using Backend.Api.Services;
using Common;
using Config.Net;
using Frontend;
using Frontend.Components;
using Microsoft.AspNetCore.Identity;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var config = new ConfigurationBuilder<IFrontendConfig>()
    .UseEnvironmentVariables()
    .UseCommandLineArgs()
    .Build();
builder.Services.AddConfiguration(config);


builder.Services.AddHttpClient<BackendClient>(
    client => client.BaseAddress = new Uri($"http://{Defaults.ServiceNames.Gateway}")
);

builder.Services.AddSingleton<ModService>();

builder.AddRedisDistributedCache(Defaults.ServiceNames.Redis);


builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

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

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();