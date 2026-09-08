using System.Security.Cryptography;
using System.Text;
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
        var config = BuildConfig(networkName, networkSecret, ipv4, credentialFile, peers, proxyListenPort);
        await File.WriteAllTextAsync(configPath, config, cancellationToken);

        var credentials = new EasytierCredentialManager(cliCommand, rpcPort);

        var processTask = Cli.Wrap(coreCommand)
            .WithArguments(["--config-file", configPath, "--rpc-portal", $"{rpcPort}"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.Out.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return new EasytierServer(credentials, processTask);
    }

    private static string BuildConfig(
        string networkName,
        string networkSecret,
        string ipv4,
        string credentialFile,
        IReadOnlyList<string> peers,
        int proxyListenPort)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            $"""
            ipv4 = "{ipv4}"
            listeners = []

            [network_identity]
            network_name = "{networkName}"
            network_secret = "{networkSecret}"

            [flags]
            no_tun = true
            private_mode = true

            credential_file = "{credentialFile}"
            """);

        foreach (var peer in peers)
        {
            builder.AppendLine(
                $"""
                [[peer]]
                uri = "{peer}"
                """);
        }

        builder.AppendLine(
            $"""
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
            """);

        return builder.ToString();
    }

    public Task WaitForExitAsync() => this.processTask.Task;
}
