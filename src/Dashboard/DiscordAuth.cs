using System.Security.Claims;
using System.Text.Json;
using Discord;
using Discord.Rest;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using StackExchange.Redis;

namespace Dashboard;

public class DiscordAuth
{
    private const string TokenSchema = "Discord";
    private const string TokenName = "access_token";

    private const string RedisRolesKeyPrefix = "DiscordRoles";
    private const string RedisAdminPrefix = "DiscordAdmin";

    private readonly IConnectionMultiplexer _con;
    private readonly IDashboardConfig _config;
    private readonly ILogger<DiscordAuth> _logger;

    public DiscordAuth(IDashboardConfig config, IConnectionMultiplexer con, ILogger<DiscordAuth> logger)
    {
        _config = config;
        _con = con;
        _logger = logger;
        if (_config.DiscordRoleId == null)
            throw new InvalidOperationException("DiscordRoleId is not configured.");
        if (_config.DiscordGuildId == null)
            throw new InvalidOperationException("DiscordGuildId is not configured.");
    }

    private static string GetRoleCacheKey(string userId)
    {
        return $"{RedisRolesKeyPrefix}:{userId}";
    }

    private static string GetAdminCacheKey(string userId)
    {
        return $"{RedisAdminPrefix}:{userId}";
    }

    private static async Task<string?> GetTokenAsync(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == false) return null;

        return await httpContext.GetTokenAsync(TokenSchema, TokenName);
    }

    private bool HasRequiredRole(IReadOnlyCollection<ulong> roles)
    {
        return roles.Contains(_config.DiscordRoleId!.Value);
    }

    private static string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<Result<bool>> IsAuthenticatedAsync(HttpContext httpContext)
    {
        var userId = GetUserId(httpContext.User);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID is not available in the claims.");
            return Result.Fail("User ID is not available in the claims.");
        }

        var token = await GetTokenAsync(httpContext);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token is not available in the context for user {UserId}", userId);
            return Result.Fail("Token is not available in the context.");
        }

        var userHasRole = await UserHasRoleAsync(userId, token);
        if (userHasRole.IsFailed)
        {
            _logger.LogError("Failed to check user role for user {UserId}: {Errors}", userId, userHasRole.Errors);
            return Result.Fail(userHasRole.Errors);
        }

        if (userHasRole.Value) return Result.Ok(true);

        var isAdmin = await IsAdmin(userId, token);
        if (isAdmin.IsFailed)
        {
            _logger.LogError("Failed to check admin status for user {UserId}: {Errors}", userId, isAdmin.Errors);
            return Result.Fail(isAdmin.Errors);
        }

        return Result.Ok(isAdmin.Value);
    }

    private async Task<bool?> CachedUserIsAdminAsync(string userId)
    {
        var db = _con.GetDatabase();
        var cacheKey = GetAdminCacheKey(userId);

        var cachedAdminStatus = await db.StringGetAsync(cacheKey);
        return bool.TryParse(cachedAdminStatus, out var isAdmin)
            ? isAdmin
            : null;
    }

    private async Task<bool?> CachedUserHasRoleAsync(string userId)
    {
        var cachedRoles = await GetCachedUserRolesAsync(userId);
        return cachedRoles?.Contains(_config.DiscordRoleId!.Value);
    }

    private async Task<List<ulong>?> GetCachedUserRolesAsync(string userId)
    {
        var db = _con.GetDatabase();
        var cacheKey = GetRoleCacheKey(userId);
        var cachedRoles = await db.StringGetAsync(cacheKey);
        if (cachedRoles.IsNullOrEmpty) return null;
        try
        {
            return JsonSerializer.Deserialize<List<ulong>>(cachedRoles!);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<Result<bool>> UserHasRoleAsync(string userId, string token)
    {
        var cachedUserHasRole = await CachedUserHasRoleAsync(userId);
        if (cachedUserHasRole != null)
        {
            _logger.LogDebug("Role cache hit for user {UserId}", userId);
            return Result.Ok(cachedUserHasRole.Value);
        }

        _logger.LogDebug("Role cache miss for user {UserId}", userId);

        var guildUserResult = await GetGuildUserAsync(token);
        if (guildUserResult.IsFailed) return Result.Fail(guildUserResult.Errors);

        var guildUser = guildUserResult.Value;
        var roles = guildUser.RoleIds.ToList();
        await CacheUserRolesAsync(userId, roles);

        return Result.Ok(HasRequiredRole(roles));
    }


    private Task CacheUserRolesAsync(string userId, IReadOnlyCollection<ulong> roles)
    {
        var db = _con.GetDatabase();
        var cacheKey = GetRoleCacheKey(userId);
        return db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(roles),
            TimeSpan.FromMinutes(5)
        );
    }

    private Task CacheUserAdminStatusAsync(string userId, bool isAdmin)
    {
        var db = _con.GetDatabase();
        var cacheKey = GetAdminCacheKey(userId);
        return db.StringSetAsync(
            cacheKey,
            isAdmin.ToString(),
            TimeSpan.FromMinutes(5)
        );
    }

    private async Task<Result<DiscordRestClient>> TryGetDiscordClientAsync(string token)
    {
        var client = new DiscordRestClient();
        try
        {
            await client.LoginAsync(TokenType.Bearer, token);
            return Result.Ok(client);
        }
        catch (Exception)
        {
            return Result
                .Fail("Failed to login to the Rest Client with Token")
                .MapErrors(error => new Error("Discord Rest Client Login Error", error));
        }
    }

    private async Task<Result<bool>> IsAdmin(string userId, string token)
    {
        var cachedUserIsAdmin = await CachedUserIsAdminAsync(userId);
        if (cachedUserIsAdmin != null)
        {
            _logger.LogDebug("Admin cache hit for user {UserId}", userId);
            return Result.Ok(cachedUserIsAdmin.Value);
        }

        _logger.LogDebug("Admin cache miss for user {UserId}", userId);

        var guildUserResult = await GetGuildUserAsync(token);
        if (guildUserResult.IsFailed) return Result.Fail(guildUserResult.Errors);

        var isAdmin = guildUserResult.Value.GuildPermissions.Administrator;
        await CacheUserAdminStatusAsync(userId, isAdmin);
        return Result.Ok(isAdmin);
    }

    private async Task<Result<RestGuildUser>> GetGuildUserAsync(string token)
    {
        var clientResult = await TryGetDiscordClientAsync(token);
        if (clientResult.IsFailed) return Result.Fail(clientResult.Errors);

        var client = clientResult.Value;
        try
        {
            var guildUser = await client.GetCurrentUserGuildMemberAsync(_config.DiscordGuildId!.Value);
            return guildUser != null
                ? Result.Ok(guildUser)
                : Result.Fail("User is not a member of the Discord guild.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guild member from Discord.");
            return Result.Fail("Exception occurred while fetching guild user.");
        }
    }
}