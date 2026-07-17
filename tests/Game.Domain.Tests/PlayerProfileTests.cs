using Game.Domain;
using Xunit;

namespace Game.Domain.Tests;

public sealed class PlayerProfileTests
{
    [Fact]
    public void CreateGuest_UsesGuestDefaults()
    {
        var userId = Guid.NewGuid();

        var profile = PlayerProfile.CreateGuest(userId, "Player123456");

        Assert.Equal(userId, profile.UserId);
        Assert.Equal("Player123456", profile.DisplayName);
        Assert.Equal(0, profile.Trophies);
        Assert.Equal(PlayerProfile.StartingGold, profile.Gold);
        Assert.Equal(AccountType.Guest, profile.AccountType);
    }
}
