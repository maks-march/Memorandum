using Microsoft.EntityFrameworkCore;
using Memorandum.Api.Domain.Organizations;
using Memorandum.Api.Domain.Users;

namespace Memorandum.Api.Infrastructure;

public sealed class MemorandumDbContext(DbContextOptions<MemorandumDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemorandumDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
