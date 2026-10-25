using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Memorandum.Tests;

public sealed class ApiVersioningTests(MemorandumApiFactory factory) : IClassFixture<MemorandumApiFactory>
{
    [Fact]
    public async Task Version_is_selected_by_url_segment()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("api-supported-versions"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("1.0", body.GetProperty("requestedVersion").GetString());
    }

    [Fact]
    public async Task Endpoint_without_version_segment_is_not_routed()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system/info");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unsupported_version_in_url_is_not_routed()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v9/system/info");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Api_version_header_is_ignored()
    {
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/info");
        request.Headers.Add("X-Api-Version", "9.9");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("v1", "1.0")]
    [InlineData("v2", "2.0")]
    public async Task Url_segment_selects_controller_version(string segment, string expected)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/test-api/{segment}/versioned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("version").GetString());
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/mock/api/health")]
    public async Task Neutral_endpoints_ignore_api_version_header(string path)
    {
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Api-Version", "9.9");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_per_version_is_published()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("paths").TryGetProperty("/api/v1/system/info", out _));
    }
}
