using System.ComponentModel.DataAnnotations;

namespace Backend.Posts;

public class Post : IDatedModel
{
    [MaxLength(250)] public required string Id { get; set; }

    [MaxLength(250)] public required string ModId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class PostTag : IDatedModel
{
    [MaxLength(250)] public required string Id { get; set; }

    [MaxLength(250)] public required string ModTagId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}