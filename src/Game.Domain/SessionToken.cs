using System.Security.Cryptography;
using System.Text;

namespace Game.Domain;

public sealed record SessionToken(Guid TokenId, Guid UserId, string TokenHash, DateTimeOffset ExpiresAt)
{
    public static SessionToken Issue(Guid userId, string rawToken, DateTimeOffset now, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Token is required.", nameof(rawToken));
        }

        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Lifetime must be positive.");
        }

        return new SessionToken(Guid.NewGuid(), userId, Hash(rawToken), now.Add(lifetime));
    }

    public bool IsValid(string rawToken, DateTimeOffset now)
    {
        return now < ExpiresAt && FixedTimeEquals(TokenHash, Hash(rawToken));
    }

    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

