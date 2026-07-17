using Game.Domain;
using Xunit;

namespace Game.Domain.Tests;

public sealed class SessionTokenTests
{
    [Fact]
    public void Issue_HashesRawToken()
    {
        var token = SessionToken.Issue(Guid.NewGuid(), "raw-token", DateTimeOffset.UtcNow, TimeSpan.FromDays(1));

        Assert.NotEqual("raw-token", token.TokenHash);
        Assert.True(token.IsValid("raw-token", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsValid_RejectsExpiredToken()
    {
        var now = DateTimeOffset.UtcNow;
        var token = SessionToken.Issue(Guid.NewGuid(), "raw-token", now, TimeSpan.FromSeconds(1));

        Assert.False(token.IsValid("raw-token", now.AddSeconds(2)));
    }

    [Fact]
    public void IsValid_RejectsTamperedToken()
    {
        var now = DateTimeOffset.UtcNow;
        var token = SessionToken.Issue(Guid.NewGuid(), "raw-token", now, TimeSpan.FromDays(1));

        Assert.False(token.IsValid("other-token", now));
    }
}
