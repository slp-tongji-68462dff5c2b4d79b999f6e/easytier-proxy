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
        if (!this.file.Exists)
            return;

        await using var stream = new FileStream(this.file.FullName, FileMode.Open, FileAccess.Read);
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

    public async Task<string> AddAsync(string username, CancellationToken cancellationToken = default)
    {
        var plainPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
        this.hashes[username] = Hash(plainPassword);
        await this.StoreAsync(cancellationToken);
        return plainPassword;
    }

    public async Task RemoveAsync(string username, CancellationToken cancellationToken = default)
    {
        this.hashes.TryRemove(username, out _);
        await this.StoreAsync(cancellationToken);
    }

    private async Task StoreAsync(CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(this.file.FullName, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, this.hashes, cancellationToken: cancellationToken);
    }

    private static byte[] Hash(string password)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        return SHA256.HashData(bytes);
    }
}
