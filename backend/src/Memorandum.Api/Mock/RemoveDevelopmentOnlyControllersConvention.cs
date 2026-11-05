using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Mocks;

internal class RemoveDevelopmentOnlyControllersConvention : IApplicationModelConvention
{
    private readonly bool _isDevelopment;

    public RemoveDevelopmentOnlyControllersConvention(IHostEnvironment env)
    {
        _isDevelopment = env.IsDevelopment();
    }

    public void Apply(ApplicationModel application)
    {
        if (_isDevelopment) return;

        // Находим все контроллеры, помеченные нашим маркерным атрибутом
        var controllersToRemove = application.Controllers
            .Where(c => c.Attributes.OfType<DevelopmentOnlyAttribute>().Any())
            .ToList();

        foreach (var controller in controllersToRemove)
        {
            application.Controllers.Remove(controller);
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
internal class DevelopmentOnlyAttribute : Attribute;