using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace EasytierProxy.Server.Proxy;

public sealed class ProxyCredentialManager
{
    private readonly FileInfo file;
    private readonly ConcurrentDictionary<string, byte[]> hashes = new();

    private ProxyCredentialManager(FileInfo file)
    {
        this.file = file;
    }

    public static async Task<ProxyCredentialManager> LoadAsync(FileInfo file, CancellationToken cancellationToken = default)
    {
        var manager = new ProxyCredentialManager(file);
        await manager.LoadAsync(cancellationToken);
        return manager;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!file.Exists)
            return;

        await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
        var entries = await JsonSerializer.DeserializeAsync<Dictionary<string, byte[]>>(stream, cancellationToken: cancellationToken) ?? [];
        foreach (var (username, hash) in entries)
        {
            this.hashes[username] = hash;
        }
    }

    public bool Verify(string username, string password)
    {
        if (!this.hashes.TryGetValue(username, out var expectedHash))
            return false;

        var actualHash = Hash(password);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public async Task AddAsync(string username, string plainPassword, CancellationToken cancellationToken = default)
    {
        this.hashes[username] = Hash(plainPassword);
        await this.StoreAsync(cancellationToken);
    }

    public async Task RemoveAsync(string username, CancellationToken cancellationToken = default)
    {
        this.hashes.TryRemove(username, out _);
        await this.StoreAsync(cancellationToken);
    }

    private async Task StoreAsync(CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
    }

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
