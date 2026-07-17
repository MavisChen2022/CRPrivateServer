using Game.Domain;
using Xunit;

namespace Game.Domain.Tests;

public sealed class FriendCodeTests
{
    [Fact]
    public void Generate_CreatesPublicSafeEightCharacterCode()
    {
        var code = FriendCode.Generate();

        Assert.Equal(8, code.Length);
        Assert.True(FriendCode.IsValid(code));
        Assert.DoesNotContain("-", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Normalize_RemovesSpacingAndUsesUppercase()
    {
        var normalized = FriendCode.Normalize(" abcd 2345 ");

        Assert.Equal("ABCD2345", normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDEFG1")]
    [InlineData("ABCDEFGI")]
    [InlineData("ABCDEFG0")]
    public void IsValid_RejectsInvalidCode(string code)
    {
        Assert.False(FriendCode.IsValid(code));
    }

    [Fact]
    public void FriendshipPair_NormalizesPlayerOrder()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var pair = FriendshipPair.Create(first, second);

        Assert.Equal(second, pair.LowerPlayerId);
        Assert.Equal(first, pair.HigherPlayerId);
    }
}

