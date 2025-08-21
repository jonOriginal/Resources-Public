using Backend.Mods;
using Backend.ModTags;
using Backend.Posts;
using Microsoft.EntityFrameworkCore;

namespace Backend;

public class DataContext(IApiConfig config, DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Mod> Mods { get; set; }

    public DbSet<ModTag> ModTags { get; set; }

    public DbSet<Post> Posts { get; set; }

    public DbSet<PostTag> PostTags { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Model.SetDefaultContainer(config.ContainerName);
    }
}