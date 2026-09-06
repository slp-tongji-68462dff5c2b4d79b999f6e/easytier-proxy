using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;

namespace EasytierProxy.Server.Credentials;

public sealed class EasytierCredentialManager(string cliBinary, string rpcPortal)
{
    public async Task<(string CredentialId, string CredentialSecret)> GenerateAsync(
        TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        var seconds = timeToLive is { } ttl
            ? (long)ttl.TotalSeconds
            : 999999999999;

        var generated = await RunCredentialAsync<GeneratedCredential>(
            ["generate", "--ttl", seconds.ToString()],
            cancellationToken);

        return (generated?.CredentialId ?? "", generated?.CredentialSecret ?? "");
    }

    public async Task<bool> RevokeAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        var result = await RunCredentialAsync<RevokeResult>(
            ["revoke", credentialId],
            cancellationToken);

        return result?.Success ?? false;
    }

    public async Task<IReadOnlyList<(string CredentialId, DateTimeOffset Expiry)>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await RunCredentialAsync<CredentialListResult>(
            ["list"],
            cancellationToken);

        var credentials = list?.Credentials ?? [];
        return credentials
            .Select(c => (c.CredentialId, DateTimeOffset.FromUnixTimeSeconds(c.ExpiryUnix)))
            .ToList();
    }

    private async Task<T?> RunCredentialAsync<T>(
        IEnumerable<string> credentialArguments,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "-o", "json", "-p", rpcPortal, "credential" };
        arguments.AddRange(credentialArguments);

        var result = await Cli.Wrap(cliBinary)
            .WithArguments(arguments)
            .ExecuteBufferedAsync(cancellationToken);

        return JsonSerializer.Deserialize<T>(result.StandardOutput, JsonSerializerOptions.Web);
    }
}
