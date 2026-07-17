using Game.Domain;

var tests = new List<(string Name, Action Body)>
{
    ("FEATURE-SESSION-001 creates guest profile defaults", CreatesGuestProfileDefaults),
    ("FEATURE-SESSION-001 hashes session token", HashesSessionToken),
    ("FEATURE-SESSION-001 rejects expired session token", RejectsExpiredSessionToken),
    ("FEATURE-SESSION-001 rejects tampered session token", RejectsTamperedSessionToken)
};

foreach (var test in tests)
{
    test.Body();
    Console.WriteLine($"PASS {test.Name}");
}

static void CreatesGuestProfileDefaults()
{
    var userId = Guid.NewGuid();
    var profile = PlayerProfile.CreateGuest(userId, "Player123456");

    AssertEqual(userId, profile.UserId);
    AssertEqual("Player123456", profile.DisplayName);
    AssertEqual(0, profile.Trophies);
    AssertEqual(PlayerProfile.StartingGold, profile.Gold);
    AssertEqual(AccountType.Guest, profile.AccountType);
}

static void HashesSessionToken()
{
    var token = SessionToken.Issue(Guid.NewGuid(), "raw-token", DateTimeOffset.UtcNow, TimeSpan.FromDays(1));

    AssertNotEqual("raw-token", token.TokenHash);
    AssertTrue(token.IsValid("raw-token", DateTimeOffset.UtcNow));
}

static void RejectsExpiredSessionToken()
{
    var now = DateTimeOffset.UtcNow;
    var token = SessionToken.Issue(Guid.NewGuid(), "raw-token", now, TimeSpan.FromSeconds(1));

    AssertFalse(token.IsValid("raw-token", now.AddSeconds(2)));
}

static void RejectsTamperedSessionToken()
{
    var now = DateTimeOffset.UtcNow;
    var token = SessionToken.Issue(Guid.NewGuid(), "raw-token", now, TimeSpan.FromDays(1));

    AssertFalse(token.IsValid("other-token", now));
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertNotEqual<T>(T unexpected, T actual)
{
    if (EqualityComparer<T>.Default.Equals(unexpected, actual))
    {
        throw new InvalidOperationException($"Did not expect {actual}.");
    }
}

static void AssertTrue(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}
