namespace Game.Domain;

public sealed record PlayerProfile(
    Guid PlayerId,
    Guid UserId,
    string DisplayName,
    int Trophies,
    int Gold,
    AccountType AccountType)
{
    public const int StartingGold = 100;

    public static PlayerProfile CreateGuest(Guid userId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        return new PlayerProfile(
            Guid.NewGuid(),
            userId,
            displayName,
            Trophies: 0,
            Gold: StartingGold,
            AccountType.Guest);
    }
}

