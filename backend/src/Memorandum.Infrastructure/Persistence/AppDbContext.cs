using Microsoft.EntityFrameworkCore;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Infrastructure.Persistence;

/// <summary>
/// Контекст БД. Модель настраивается в <c>Configurations/</c>, репозитории — в <c>Repositories/</c>.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> Memberships => Set<OrganizationMembership>();

    public DbSet<Credential> Credentials => Set<Credential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
