using CliWrap;

namespace EasytierProxy.Client.Easytier;

public sealed class EasytierClient
{
    private readonly CommandTask<CommandResult> processTask;

    private EasytierClient(CommandTask<CommandResult> processTask)
    {
        this.processTask = processTask;
    }

    public static EasytierClient Start(
        string coreCommand,
        string networkName,
        string credentialSecret,
        IReadOnlyList<string> peers,
        int socks5Port,
        int rpcPort,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "--network-name", networkName,
            "--credential", credentialSecret,
            "--secure-mode", "true",
            "--no-tun",
            "--socks5", socks5Port.ToString(),
            "--rpc-portal", rpcPort.ToString(),
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
            .ExecuteAsync(cancellationToken);

        return new EasytierClient(processTask);
    }

    public Task WaitForExitAsync() => this.processTask.Task;
}
