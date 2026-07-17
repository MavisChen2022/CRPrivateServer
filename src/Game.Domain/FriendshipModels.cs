namespace Game.Domain;

public static class FriendCode
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(chars);
    }

    public static string Normalize(string code)
    {
        return new string(code
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    public static bool IsValid(string code)
    {
        var normalized = Normalize(code);
        return normalized.Length == 8 && normalized.All(Alphabet.Contains);
    }
}

public sealed record FriendshipPair(Guid LowerPlayerId, Guid HigherPlayerId)
{
    public static FriendshipPair Create(Guid firstPlayerId, Guid secondPlayerId)
    {
        return firstPlayerId.CompareTo(secondPlayerId) <= 0
            ? new FriendshipPair(firstPlayerId, secondPlayerId)
            : new FriendshipPair(secondPlayerId, firstPlayerId);
    }
}

