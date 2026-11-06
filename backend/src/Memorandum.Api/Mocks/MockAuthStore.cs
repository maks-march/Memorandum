using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Memorandum.Api.Mocks;

public sealed class MockAuthStore
{
    private readonly Lock _gate = new();
    private readonly List<MockUser> _users = MockAuthFixtures.Users().ToList();
    private readonly ConcurrentDictionary<string, MockSession> _sessions = new();


    public Option<MockUser> FindByEmail(string email)
    {
        lock (_gate)
        {
            return UnlockedFindByEmail(email);
        }
    }

    public Fin<MockUser> Authenticate(string email, string password) => 
        FindByEmail(email)
            .ToFin(MockErrors.InvalidCredentials)
            .Bind(user => user.Password == password
                ? Fin<MockUser>.Succ(user)
                : Fin<MockUser>.Fail(MockErrors.InvalidCredentials));

    public Fin<MockSession> CreateSession(MockUser user)
    {
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var xsrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var session = new MockSession(
            sessionId,
            user.Id,
            DateTimeOffset.UtcNow.AddHours(8),
            xsrf);
        _sessions[sessionId] = session;

        return session;
    }

    public Option<MockSession> FindSession(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) && session.ExpiresAt > DateTimeOffset.UtcNow 
            ? Option<MockSession>.Some(session) 
            : Option<MockSession>.None;

    public Fin<MockUser> RequireUser(string? sessionId) =>
        FindSession(sessionId ?? string.Empty)
            .ToFin(MockErrors.SessionNotFound)
            .Bind(session => 
                Prelude
                .Optional(
                    _users.FirstOrDefault(u => u.Id == session.UserId))
                .ToFin(MockErrors.SessionNotFound));

    public Fin<Unit> RequireXsrf(string sessionId, string? token) =>
        FindSession(sessionId)
            .ToFin(MockErrors.SessionNotFound)
            .Bind(session => string.Equals(session.XsrfToken, token, StringComparison.Ordinal)
                ? Fin<Unit>.Succ(Unit.Default)
                : Fin<Unit>.Fail(MockErrors.InvalidXsrf));


    public void RemoveSession(string sid) => _sessions.TryRemove(sid, out _);

    public Fin<MockUser> AddUser(string email, string password, string fullName, Guid? orgId)
    {
        lock (_gate)
        {
            if (UnlockedFindByEmail(email).IsSome)
                return MockErrors.EmailTaken(email);

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

    private Option<MockUser> UnlockedFindByEmail(string email) =>
        _users.FirstOrDefault(user => string.Equals(user.Email, email,
            StringComparison.OrdinalIgnoreCase));
}