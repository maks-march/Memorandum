using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Убирает контроллеры моков из приложения, когда заглушки выключены:
/// маршрута <c>/api/v1/mocks/...</c> в проде нет вовсе.
/// </summary>
internal sealed class MocksExcludedProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private const string MockNamespace = "Memorandum.Api.Controllers.Mocks";

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var controller in feature.Controllers
                     .Where(type => type.Namespace == MockNamespace)
                     .ToArray())
            feature.Controllers.Remove(controller);
    }
}
