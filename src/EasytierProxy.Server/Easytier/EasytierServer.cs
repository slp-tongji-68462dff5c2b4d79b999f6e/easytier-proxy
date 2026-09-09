using System.Security.Cryptography;
using CliWrap;

namespace EasytierProxy.Server.Easytier;

public sealed class EasytierServer
{
    private readonly CommandTask<CommandResult> processTask;

    private EasytierServer(EasytierCredentialManager credentials, CommandTask<CommandResult> processTask)
    {
        this.Credentials = credentials;
        this.processTask = processTask;
    }

    public EasytierCredentialManager Credentials { get; }

    public static async Task<EasytierServer> StartAsync(
        string coreCommand,
        string cliCommand,
        string networkName,
        string ipv4,
        int rpcPort,
        IReadOnlyList<string> peers,
        int proxyListenPort,
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

        var credentialFile = Path.Combine(easytierDataDirectory.FullName, "credentials.json");
        var configPath = Path.Combine(easytierDataDirectory.FullName, "config.toml");
        var config =
            $"""
            [network_identity]
            network_name = "{networkName}"
            network_secret = "{networkSecret}"

            [[acl.acl_v1.chains]]
            name = "inbound"
            chain_type = 1
            enabled = true
            default_action = 2

            [[acl.acl_v1.chains.rules]]
            name = "allow-proxy"
            enabled = true
            protocol = 5
            ports = ["{proxyListenPort}"]
            action = 1
            """;
        await File.WriteAllTextAsync(configPath, config, cancellationToken);

        var credentials = new EasytierCredentialManager(cliCommand, rpcPort);

        var arguments = new List<string>
        {
            "--config-file", configPath,
            "--rpc-portal", $"{rpcPort}",
            "--secure-mode",
            "--ipv4", ipv4,
            "--no-listener",
            "--no-tun",
            "--private-mode", "true",
            "--credential-file", credentialFile,
        };
        foreach (var peer in peers)
        {
            arguments.Add("--peers");
            arguments.Add(peer);
        }

        var processTask = Cli.Wrap(coreCommand)
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.Out.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return new EasytierServer(credentials, processTask);
    }

    public Task WaitForExitAsync() => this.processTask.Task;
}
