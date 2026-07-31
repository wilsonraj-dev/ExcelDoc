using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Tests;

public sealed class SapSessionStoreTests
{
    [Fact]
    public void Add_StoresActiveSession()
    {
        var store = new SapSessionStore();
        var expected = CreateSession("active", DateTime.UtcNow.AddMinutes(5));

        store.Add(expected);

        var found = store.TryGet(expected.SessionKey, out var actual);
        Assert.True(found);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void TryGet_RemovesExpiredSession()
    {
        var store = new SapSessionStore();
        var expired = CreateSession("expired", DateTime.UtcNow.AddMinutes(-5));
        store.Add(expired);

        var firstLookup = store.TryGet(expired.SessionKey, out var firstResult);
        var secondLookup = store.TryGet(expired.SessionKey, out var secondResult);

        Assert.False(firstLookup);
        Assert.Null(firstResult);
        Assert.False(secondLookup);
        Assert.Null(secondResult);
    }

    [Fact]
    public void Add_ReplacesSessionWithSameKey()
    {
        var store = new SapSessionStore();
        var original = CreateSession("same-key", DateTime.UtcNow.AddMinutes(5));
        var replacement = CreateSession("same-key", DateTime.UtcNow.AddMinutes(10), database: "SBOTEST");
        store.Add(original);

        store.Add(replacement);

        Assert.True(store.TryGet("same-key", out var actual));
        Assert.Same(replacement, actual);
        Assert.Equal("SBOTEST", actual!.Database);
    }

    [Fact]
    public void Remove_ReturnsAndDeletesSession()
    {
        var store = new SapSessionStore();
        var expected = CreateSession("logout", DateTime.UtcNow.AddMinutes(5));
        store.Add(expected);

        var removed = store.Remove(expected.SessionKey, out var actual);

        Assert.True(removed);
        Assert.Same(expected, actual);
        Assert.False(store.TryGet(expected.SessionKey, out _));
    }

    [Fact]
    public void Add_RejectsNullSession()
    {
        var store = new SapSessionStore();

        Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    }

    [Fact]
    public void Logout_IsDeferredUntilActiveJobReleasesItsLease()
    {
        var store = new SapSessionStore();
        var session = CreateSession("job", DateTime.UtcNow.AddMinutes(5));
        store.Add(session);

        Assert.True(store.TryAcquireJob(session.SessionKey));
        Assert.Null(store.RequestLogout(session.SessionKey));
        Assert.False(store.TryGet(session.SessionKey, out _));
        Assert.True(store.TryGetForJob(session.SessionKey, out var jobSession));
        Assert.Same(session, jobSession);

        var sessionToLogout = store.ReleaseJob(session.SessionKey);

        Assert.Same(session, sessionToLogout);
        Assert.False(store.TryGetForJob(session.SessionKey, out _));
    }

    [Fact]
    public void RemoveExpired_PreservesActiveJobAndRemovesItAfterRelease()
    {
        var store = new SapSessionStore();
        var session = CreateSession("expiring-job", DateTime.UtcNow.AddMinutes(5));
        store.Add(session);
        Assert.True(store.TryAcquireJob(session.SessionKey));
        session.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        Assert.Empty(store.RemoveExpired());
        Assert.True(store.TryGetForJob(session.SessionKey, out _));
        Assert.Same(session, store.ReleaseJob(session.SessionKey));
    }

    [Fact]
    public void RemoveExpired_ReturnsInactiveSessionsForBestEffortLogout()
    {
        var store = new SapSessionStore();
        var expired = CreateSession("sweep", DateTime.UtcNow.AddMinutes(-1));
        store.Add(expired);

        var removed = store.RemoveExpired();

        Assert.Single(removed);
        Assert.Same(expired, removed.Single());
    }

    private static SapSessionContext CreateSession(
        string sessionKey,
        DateTime expiresAtUtc,
        string database = "SBOPROD") =>
        new()
        {
            SessionKey = sessionKey,
            ServiceLayerBaseUrl = "https://sap.example.test:50000/b1s/v1/",
            Database = database,
            UserName = "manager",
            SessionTimeoutMinutes = 30,
            ExpiresAtUtc = expiresAtUtc
        };
}
