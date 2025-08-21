using Backend;
using Backend.Event.Services;
using Backend.Mods;
using Backend.ModTags;
using Backend.NamedPosts;
using Backend.Posts;
using Common;
using Config.Net;
using Microsoft.EntityFrameworkCore;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var config = new ConfigurationBuilder<IApiConfig>()
    .UseEnvironmentVariables()
    .UseCommandLineArgs()
    .Build();

builder.Services.AddConfiguration(config);

builder.AddRedisDistributedCache(Defaults.ServiceNames.Redis);

builder.Services.AddDbContextFactory<DataContext>(options =>
{
    options.UseCosmos
        (
            config.ConnectionString ?? throw new InvalidOperationException("Connection string is not set."),
            config.DatabaseName ?? throw new InvalidOperationException("Database name is not set.")
        )
        .EnableSensitiveDataLogging();
});


builder.Services.AddSingleton<ModService>();
builder.Services.AddSingleton<PostService>();
builder.Services.AddSingleton<NamedPostService>();
builder.Services.AddSingleton<ModTagService>();
builder.Services.AddSingleton<EventProducer>();

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();