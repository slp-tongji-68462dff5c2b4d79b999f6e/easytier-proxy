using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace EasytierProxy.Server;

public sealed class ProxyUserRegistry
{
    private readonly ConcurrentDictionary<string, byte[]> hashes = new();

    public bool Verify(string username, string password)
    {
        if (!this.hashes.TryGetValue(username, out var expectedHash))
            return false;

        var actualHash = Hash(password);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public void Add(string username, string plainPassword)
    {
        this.hashes[username] = Hash(plainPassword);
    }

    public bool Remove(string username) => this.hashes.TryRemove(username, out _);

    private static byte[] Hash(string password)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        return SHA256.HashData(bytes);
    }

    public static string GeneratePassword()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
    }
}
