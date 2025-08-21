using System.ComponentModel.DataAnnotations;

namespace Backend.Mods;

public class Mod : IDatedModel
{
    [MaxLength(250)] public required string Id { get; set; }

    [MaxLength(250)] public required string Name { get; set; }

    [MaxLength(250)] public required string Author { get; set; }

    [MaxLength(12000)] public required string? Description { get; set; }

    [MaxLength(500)] public required string? DiscordUrl { get; set; }

    [MaxLength(500)] public required string? WebsiteUrl { get; set; }

    [MaxLength(500)] public required string? IconUrl { get; set; }

    public required float Rating { get; set; }

    public required bool IsCompromised { get; set; }

    public IList<string>? TagIds { get; set; }

    public IList<string>? UpdatedByUserIds { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}