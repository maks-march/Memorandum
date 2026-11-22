using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Memorandum.Infrastructure.Persistence;

/// <summary>
/// Контекст для <c>dotnet ef</c>: миграции строятся только для Postgres.
/// Строка подключения здесь не используется — нужна лишь провайдеру для генерации SQL.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=memorandum;Username=memorandum;Password=memorandum")
            .Options);
}
