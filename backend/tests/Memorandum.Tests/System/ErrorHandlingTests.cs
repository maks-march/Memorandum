using System.Net;
using System.Text.Json;

namespace Memorandum.Tests;

public sealed class ErrorHandlingTests(MemorandumApiFactory factory) : IClassFixture<MemorandumApiFactory>
{
    [Fact]
    public async Task Unhandled_exception_returns_500_with_problem_details_and_trace_id()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test-api/boom");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.True(problem.GetProperty("traceId").GetString() is { Length: > 0 });
    }
}
