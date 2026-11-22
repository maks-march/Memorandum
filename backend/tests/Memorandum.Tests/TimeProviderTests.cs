using System.Net;
using System.Net.Http.Json;

namespace Memorandum.Tests;

/// <summary>Проверяет, что время в приложении берётся из зарегистрированного <see cref="TimeProvider"/>.</summary>
public sealed class TimeProviderTests
{
    [Fact]
    public async Task Health_reports_time_from_registered_provider()
    {
        var moment = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        using var factory = new MemorandumApiFactory { TimeProvider = new TestTimeProvider(moment) };
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var body = await response.Content.ReadFromJsonAsync<HealthPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(moment.UtcDateTime, body!.TimeUtc);
        Assert.Equal("api", body.Mode);
    }

    private sealed record HealthPayload(string Status, string Mode, DateTime TimeUtc);
}
