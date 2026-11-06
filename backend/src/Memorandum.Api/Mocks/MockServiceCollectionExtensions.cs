namespace Memorandum.Api.Mocks;

internal static class MockServiceCollectionExtensions
{
    internal static IServiceCollection AddMockServices(this IServiceCollection services)
    {
        services.AddSingleton<MockAuthStore>();
        return services;
    }
}