using System.Security.Cryptography;
using Lemon.Application.Abstractions.Security;

namespace Lemon.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expected = Convert.FromBase64String(hash);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
            return expected.Length == HashSize && CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
