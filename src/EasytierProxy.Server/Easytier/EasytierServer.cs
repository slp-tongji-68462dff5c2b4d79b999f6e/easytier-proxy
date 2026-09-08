using System.Security.Cryptography;
using CliWrap;

namespace EasytierProxy.Server.Easytier;

public sealed class EasytierServer(
    EasytierCredentialManager credentials,
    CommandTask<CommandResult> processTask)
{
    public EasytierCredentialManager Credentials { get; } = credentials;

    public static async Task<EasytierServer> RunAsync(
        string coreCommand,
        string cliCommand,
        string networkName,
        int rpcPort,
        IReadOnlyList<string> peers,
        DirectoryInfo easytierDataDirectory,
        CancellationToken cancellationToken = default)
    {
        easytierDataDirectory.Create();

        var networkSecretPath = Path.Combine(easytierDataDirectory.FullName, "network-secret.txt");
        string networkSecret;
        if (File.Exists(networkSecretPath))
        {
            networkSecret = await File.ReadAllTextAsync(networkSecretPath, cancellationToken);
        }
        else
        {
            networkSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await File.WriteAllTextAsync(networkSecretPath, networkSecret, cancellationToken);
        }

        var credentials = new EasytierCredentialManager(cliCommand, rpcPort);

        var arguments = new List<string>
        {
            "--network-name", networkName,
            "--network-secret", networkSecret,
            "--rpc-portal", $"{rpcPort}",
            "--credential-file", Path.Combine(easytierDataDirectory.FullName, "credentials.json"),
        };

        foreach (var peer in peers)
        {
            arguments.Add("--peers");
            arguments.Add(peer);
        }

        arguments.Add("--no-listener");
        arguments.Add("--no-tun");

        var processTask = Cli.Wrap(coreCommand)
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.Out.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return new EasytierServer(credentials, processTask);
    }

    public Task WaitForExitAsync() => processTask.Task;
}
