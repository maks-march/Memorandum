using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Memorandum.Infrastructure.Persistence.DataProtection;

/// <summary>
/// Хранилище ключей Data Protection в БД: у контейнеров нет постоянного диска,
/// а ключи должны переживать деплой и быть общими для всех реплик.
/// Репозиторий живёт дольше контекста, поэтому каждый вызов создаёт свой scope.
/// </summary>
public sealed class EfXmlRepository(IServiceScopeFactory scopeFactory) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = db.DataProtectionKeys
            .AsNoTracking()
            .OrderBy(key => key.Id)
            .Select(key => key.Xml)
            .ToList();

        return stored.Select(XElement.Parse).ToList();
    }

    public void StoreElement(XElement element, string? friendlyName)
    {
        using var scope = scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var name = string.IsNullOrWhiteSpace(friendlyName) ? $"key-{Guid.NewGuid():N}" : friendlyName;

        db.DataProtectionKeys.Add(DataProtectionKey.Create(name, element));
        db.SaveChanges();
    }
}
