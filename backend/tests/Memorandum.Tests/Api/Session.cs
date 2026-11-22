using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Memorandum.Api.Contracts;

namespace Memorandum.Tests.Infrastructure.Api;

/// <summary>
/// Клиент с ручным управлением cookie: тесты видят, какие cookie выставил сервер,
/// и сами решают, что отправить в следующем запросе.
/// </summary>
internal sealed class Session(HttpClient client)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, string> _cookies = [];

    public static async Task<Session> StartAsync(HttpClient client)
    {
        var session = new Session(client);
        await session.RefreshCsrfAsync();
        return session;
    }

    public string? XsrfToken { get; private set; }

    /// <summary>Cookie, которые сейчас есть у клиента. XSRF-TOKEN нужен для отправки токена.</summary>
    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public string? AuthCookieName { get; private set; }

    /// <summary>Имитация клиента, потерявшего токен: заголовок не отправляется.</summary>
    public void ClearCsrfToken() => XsrfToken = null;

    /// <summary>Подмена токена: проверяем реакцию на несовпадение с cookie.</summary>
    public void SetRawToken(string token) => XsrfToken = token;

    public async Task RefreshCsrfAsync()
    {
        using var response = await client.GetAsync("/api/v1/auth/csrf");
        response.EnsureSuccessStatusCode();

        CaptureCookies(response);
        XsrfToken = (await response.Content.ReadFromJsonAsync<CsrfHttpResponse>(Json))!.XsrfToken;
    }

    public async Task<long> RegisterAsync(string email, string? organizationName = null)
    {
        using var response = await PostAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "password123", "Иван Петров", organizationName));

        var problem = response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Регистрация не прошла: {problem}");

        var body = await response.Content.ReadFromJsonAsync<MeHttpResponse>(Json);
        return body!.Id;
    }

    public Task<LoginHttpResponse> LoginAsync(string email, string password = "password123") =>
        LoginAsync(new LoginHttpRequest(email, password));

    /// <summary>Попытка входа без ожидания успеха: тесту нужен только статус.</summary>
    public async Task<HttpStatusCode> TryLoginAsync(string email, string password)
    {
        using var response = await PostAsync("/api/v1/auth/login", new LoginHttpRequest(email, password));
        return response.StatusCode;
    }

    public async Task<LoginHttpResponse> LoginAsync(LoginHttpRequest request)
    {
        using var response = await PostAsync("/api/v1/auth/login", request);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Вход не прошёл: {raw}");

        var body = JsonSerializer.Deserialize<LoginHttpResponse>(raw, Json)!;
        XsrfToken = body.XsrfToken;
        AuthCookieName = _cookies.Keys.SingleOrDefault(key => key.Contains("memo.sid"));

        return body;
    }

    public Task<HttpResponseMessage> GetAsync(string path) => SendAsync(HttpMethod.Get, path, content: null);

    public Task<HttpResponseMessage> PostAsync<T>(string path, T body) =>
        SendAsync(HttpMethod.Post, path, JsonContent.Create(body));

    public Task<HttpResponseMessage> PostAsync(string path, HttpContent? body) =>
        SendAsync(HttpMethod.Post, path, body);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, path);

        if (content is not null)
            request.Content = content;

        if (_cookies.Count > 0)
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(pair => $"{pair.Key}={pair.Value}")));

        if (XsrfToken is not null)
            request.Headers.Add("X-XSRF-TOKEN", XsrfToken);

        var response = await client.SendAsync(request);
        CaptureCookies(response);
        return response;
    }

    private void CaptureCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return;

        foreach (var raw in values)
        {
            var pair = raw.Split(';')[0];
            var separator = pair.IndexOf('=');
            var name = pair[..separator];
            var value = pair[(separator + 1)..];

            if (value.Length == 0 || raw.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase))
                _cookies.Remove(name);
            else
                _cookies[name] = value;
        }
    }
}
