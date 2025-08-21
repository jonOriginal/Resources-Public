using System.ComponentModel.DataAnnotations;

namespace Backend.ModTags;

public class ModTag
{
    [MaxLength(50)] public required string Id { get; set; }

    [MaxLength(50)] public required string Name { get; set; }

    [MaxLength(500)] public required string Description { get; set; }

    [MaxLength(7)] public required string ColorHex { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}