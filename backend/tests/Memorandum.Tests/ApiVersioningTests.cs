using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Memorandum.Tests;

public sealed class ApiVersioningTests(MemorandumApiFactory factory) : IClassFixture<MemorandumApiFactory>
{
    private static HttpRequestMessage Request(string path, string? apiVersion)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (apiVersion is not null)
            request.Headers.Add("X-Api-Version", apiVersion);
        return request;
    }

    [Fact]
    public async Task Versioned_endpoint_without_header_uses_default_version()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(Request("/api/system/info", apiVersion: null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("api-supported-versions"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("1.0", body.GetProperty("requestedVersion").GetString());
    }

    [Fact]
    public async Task Versioned_endpoint_with_valid_header_returns_200()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(Request("/api/system/info", "1.0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Versioned_endpoint_with_unsupported_header_returns_400_problem_details()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(Request("/api/system/info", "9.9"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("1.0", "1.0")]
    [InlineData("2.0", "2.0")]
    public async Task Header_selects_controller_version(string requested, string expected)
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(Request("/test-api/versioned", requested));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Neutral_endpoints_work_without_header_but_reject_unknown_version()
    {
        var client = factory.CreateClient();

        var withoutHeader = await client.SendAsync(Request("/api/health", apiVersion: null));
        var withKnownVersion = await client.SendAsync(Request("/api/health", "1.0"));
        var withBogusVersion = await client.SendAsync(Request("/api/health", "9.9"));

        Assert.Equal(HttpStatusCode.OK, withoutHeader.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withKnownVersion.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, withBogusVersion.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_per_version_is_published()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("paths").TryGetProperty("/api/system/info", out _));
    }
}
