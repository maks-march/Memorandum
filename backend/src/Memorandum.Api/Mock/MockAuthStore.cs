using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Memorandum.Api.Mocks;

internal sealed class MockAuthStore
{
    private readonly Lock _gate = new();
    private readonly List<MockUser> _users = MockAuthFixtures.Users().ToList();
    private readonly ConcurrentDictionary<string, MockSession> _sessions = new();


    public MockUser? FindByEmail(string email)
    {
        lock (_gate)
        {
            return UnlockedFindByEmail(email);
        }
    }

    public bool CheckPassword(MockUser u, string p)
        => u.Password == p;

    public (string sid, string xsrf) CreateSession(MockUser user)
    {
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var xsrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        _sessions[sessionId] = new MockSession(
            user.Id,
            DateTimeOffset.UtcNow.AddHours(8),
            xsrf);

        return (sessionId, xsrf);
    }

    public MockUser? GetUserBySession(string sid)
    {
        _sessions.TryGetValue(sid, out var session);
        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;
        var userId = session?.UserId ?? Guid.Empty;
        return _users.FirstOrDefault(u => u.Id == userId);
    }

    public bool ValidateXsrf(string sid, string? token)
    {
        _sessions.TryGetValue(sid, out var session);
        return session is not null && session.XsrfToken == token;
    }

    public void RemoveSession(string sid) => _sessions.TryRemove(sid, out _);

    public MockUser AddUser(string email, string password, string fullName, Guid? orgId)
    {
        lock (_gate)
        {
            if (UnlockedFindByEmail(email) is not null)
                throw new InvalidOperationException($"email_taken: {email}");

            var user = new MockUser(
                Guid.NewGuid(), email, password, fullName,
                Position: "Инженер-проектировщик", Role: "OA", OrgId: orgId);

            _users.Add(user);
            return user;
        }
    }

    public void Reset()
    {
        _sessions.Clear();
        lock (_gate)
        {
            _users.Clear();
            _users.AddRange(MockAuthFixtures.Users());
        }
    }

    private MockUser? UnlockedFindByEmail(string email) =>
        _users.FirstOrDefault(user => string.Equals(user.Email, email,
            StringComparison.OrdinalIgnoreCase));
}