namespace ServiceDefaults;

public static class Defaults
{
    public static class ServiceNames
    {
        public const string Gateway = "backend";

        public const string Redis = "redis";

        public const string Dashboard = "dashboard";

        public const string Frontend = "frontend";

        public const string DiscordBot = "bot";

        public const string Cloudflare = "cloudflare";
    }


    public static class Env
    {
        public const string DiscordAppId = "DISCORDAPPID";

        public const string DiscordAppSecret = "DISCORDAPPSECRET";

        public const string CosmosConnectionString = "COSMOSCONNECTIONSTRING";

        public const string CosmosDatabaseName = "COSMOSDATABASENAME";

        public const string CosmosContainerName = "COSMOSCONTAINERNAME";

        public const string DiscordInviteLink = "DISCORDINVITELINK";

        public const string DiscordAllowedRoleId = "DISCORDROLEID";

        public const string DiscordGuildId = "DISCORDGUILDID";

        public const string DiscordBotToken = "DISCORDBOTTOKEN";

        public const string DiscordNotificationChannelId = "NOTIFICATIONCHANNELID";

        public const string CloudflareTunnelToken = "TUNNEL_TOKEN";

        public const string CloudflareTunnelName = "TUNNEL_NAME";

        public const string DiscordModForumChannelId = "DISCORDMODFORUMCHANNELID";
        
        public const string DiscordUpdateChannelId = "DISCORDUPDATECHANNELID";
    }
}