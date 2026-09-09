using System.Diagnostics;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;

namespace EasytierProxy.Server.Easytier;

public sealed class EasytierCredentialManager(string cliBinary, int rpcPort)
{
    private static readonly DateTimeOffset MaxExpiry = DateTimeOffset.MaxValue.AddYears(-1);

    public async Task<(string CredentialId, string CredentialSecret, DateTimeOffset Expiry)> GenerateAsync(
        DateTimeOffset? expiry,
        CancellationToken cancellationToken = default)
    {
        if (!expiry.HasValue || expiry > MaxExpiry)
        {
            expiry = MaxExpiry;
        }
        var seconds = (long)(expiry.Value - DateTimeOffset.UtcNow).TotalSeconds;

        if (seconds < 60)
        {
            seconds = 60;
        }

        var generated = await RunCredentialAsync<GeneratedCredential>(
            ["generate", "--ttl", seconds.ToString()],
            cancellationToken);

        Debug.Assert(generated is not null);

        var actualExpiry = (await ListAsync(cancellationToken)
            .SingleAsync(c => c.CredentialId == generated.CredentialId, cancellationToken)).Expiry;

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

    public async IAsyncEnumerable<(string CredentialId, DateTimeOffset Expiry)> ListAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = await RunCredentialAsync<CredentialListResult>(
            ["list"],
            cancellationToken);

        Debug.Assert(list is not null);

        foreach (var credential in list.Credentials)
        {
            yield return (credential.CredentialId, DateTimeOffset.FromUnixTimeSeconds(credential.ExpiryUnix));
        }
    }

    private async Task<T?> RunCredentialAsync<T>(
        IEnumerable<string> credentialArguments,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "--output", "json", "--rpc-portal", $"127.0.0.1:{rpcPort}", "credential" };
        arguments.AddRange(credentialArguments);

        var result = await Cli.Wrap(cliBinary)
            .WithArguments(arguments)
            .ExecuteBufferedAsync(cancellationToken);

        return JsonSerializer.Deserialize<T>(result.StandardOutput, JsonSerializerOptions.Web);
    }
}
