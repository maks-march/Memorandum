using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Mocks;

internal sealed class MockAuthStore
{
    private readonly Lock _gate = new();
    private readonly List<MockUser> _users = MockAuthFixtures.Users().ToList();
    private readonly ConcurrentDictionary<string, MockSession> _sessions = new();


    MockUser? FindByEmail(string email)
        => _users.FirstOrDefault(user => string.Equals(user.Email, email, 
            StringComparison.OrdinalIgnoreCase));
            
    bool CheckPassword(MockUser u, string p)
        => u.Password == p;  

    (string sid, string xsrf) CreateSession(MockUser user)
    {
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var xsrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        _sessions[sessionId] = new MockSession(
            user.Id,
            DateTimeOffset.UtcNow.AddHours(8),
            xsrf);

        return (sessionId, xsrf);
    }

    MockUser? GetUserBySession(string sid)
    {
        _sessions.TryGetValue(sid, out var session);
        var userId = session?.UserId ?? Guid.Empty;
        return _users.FirstOrDefault(u => u.Id == userId);
    }

    bool ValidateXsrf(string sid, string? token)
    {
        _sessions.TryGetValue(sid, out var session);
        return session is not null && session.XsrfToken == token;
    }

    void RemoveSession(string sid) => _sessions.TryRemove(sid, out _);

    MockUser AddUser(string email, string password, string fullName, Guid? orgId)
    {
        lock (_gate)
        {
            if (FindByEmail(email) is not null)
                throw new InvalidOperationException($"email_taken: {email}");

            var user = new MockUser(
                Guid.NewGuid(), email, password, fullName,
                Position: "Инженер-проектировщик", Role: "OA", OrgId: orgId);

            _users.Add(user);
            return user;
        }
    }
    
    void Reset()
    {
        lock (_gate)
        {
            _users.Clear();
            _users.AddRange(MockAuthFixtures.Users());
            _sessions.Clear();
        }
    }
}