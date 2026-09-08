using System.Diagnostics;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;

namespace EasytierProxy.Server.Easytier;

public sealed class EasytierCredentialManager(string cliBinary, int rpcPort)
{
    public async Task<(string CredentialId, string CredentialSecret, DateTimeOffset Expiry)> GenerateAsync(
        DateTimeOffset? expiry,
        CancellationToken cancellationToken = default)
    {
        var effectiveExpiry = expiry ?? DateTimeOffset.UtcNow.AddSeconds(999999999999);
        var seconds = (long)(effectiveExpiry - DateTimeOffset.UtcNow).TotalSeconds;
        if (seconds > 999999999999)
        {
            seconds = 999999999999;
        }

        if (seconds < 60)
        {
            seconds = 60;
        }

        var generated = await RunCredentialAsync<GeneratedCredential>(
            ["generate", "--ttl", seconds.ToString()],
            cancellationToken);

        Debug.Assert(generated is not null);

        var listed = await ListAsync(cancellationToken);
        var actualExpiry = listed.First(c => c.CredentialId == generated.CredentialId).Expiry;

        return (
            generated.CredentialId,
            generated.CredentialSecret,
            actualExpiry);
    }

    public async Task<bool> RevokeAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        var result = await RunCredentialAsync<RevokeResult>(
            ["revoke", credentialId],
            cancellationToken);

        Debug.Assert(result is not null);
        return result.Success;
    }

    public async Task<IReadOnlyList<(string CredentialId, DateTimeOffset Expiry)>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await RunCredentialAsync<CredentialListResult>(
            ["list"],
            cancellationToken);

        Debug.Assert(list is not null);

        return list.Credentials
            .Select(c => (c.CredentialId, DateTimeOffset.FromUnixTimeSeconds(c.ExpiryUnix)))
            .ToList();
    }

    private async Task<T?> RunCredentialAsync<T>(
        IEnumerable<string> credentialArguments,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "-o", "json", "-p", $"127.0.0.1:{rpcPort}", "credential" };
        arguments.AddRange(credentialArguments);

        var result = await Cli.Wrap(cliBinary)
            .WithArguments(arguments)
            .ExecuteBufferedAsync(cancellationToken);

        return JsonSerializer.Deserialize<T>(result.StandardOutput, JsonSerializerOptions.Web);
    }
}
