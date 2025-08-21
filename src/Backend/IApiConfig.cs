using Analysers.Attributes;
using Config.Net;
using ServiceDefaults;

namespace Backend;

[Config]
public interface IApiConfig
{
    [Option(Alias = Defaults.Env.CosmosConnectionString)]
    string? ConnectionString { get; }

    [Option(Alias = Defaults.Env.CosmosDatabaseName)]
    string? DatabaseName { get; }

    [Option(Alias = Defaults.Env.CosmosContainerName)]
    string? ContainerName { get; }
}