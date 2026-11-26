using Microsoft.EntityFrameworkCore;
using Memorandum.Domain.Documents;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Projects;
using Memorandum.Domain.Schemas;
using Memorandum.Domain.Users;
using Memorandum.Infrastructure.Persistence.DataProtection;

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

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentSection> DocumentSections => Set<DocumentSection>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<DocumentImport> DocumentImports => Set<DocumentImport>();

    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();

    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();

    /// <summary>Ключи Data Protection: cookie и анти-CSRF токены переживают рестарт.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
