using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using EasytierProxy.Server.CredentialManagement;
using EasytierProxy.Server.Easytier;
using EasytierProxy.Server.Proxy;

namespace EasytierProxy.Server.Commands;

[Command("run")]
public sealed partial class RunCommand : ICommand
{
    [CommandOption("easytier-core-command")]
    public string EasytierCoreCommand { get; set; } = "easytier-core";

    [CommandOption("easytier-cli-command")]
    public string EasytierCliCommand { get; set; } = "easytier-cli";

    [CommandOption("easytier-network-name")]
    public required string EasytierNetworkName { get; set; }

    [CommandOption("easytier-ipv4")]
    public required string EasytierIpv4 { get; set; }

    [CommandOption("easytier-rpc-port")]
    public required int EasytierRpcPort { get; set; }

    [CommandOption("easytier-peer")]
    public required IReadOnlyList<string> EasytierPeers { get; set; }

    [CommandOption("proxy-port")]
    public required int ProxyPort { get; set; }

    [CommandOption("credential-manager-port")]
    public required int CredentialManagerPort { get; set; }

    [CommandOption("data-directory")]
    public required string DataDirectory { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var dataDirectory = new DirectoryInfo(DataDirectory);

        var easytierServer = await EasytierServer.StartAsync(
            EasytierCoreCommand,
            EasytierCliCommand,
            EasytierNetworkName,
            EasytierIpv4,
            EasytierRpcPort,
            EasytierPeers,
            ProxyPort,
            new DirectoryInfo(Path.Combine(dataDirectory.FullName, "easytier")));

        await using var proxy = await ProxyServer.CreateAsync(
            ProxyPort,
            new DirectoryInfo(Path.Combine(dataDirectory.FullName, "proxy")));
        await using var credentialManageServer = await CredentialManageServer.StartAsync(
            easytierServer.Credentials, proxy.Credentials, CredentialManagerPort);

        await console.Output.WriteLineAsync($"Proxy listening on 127.0.0.1:{ProxyPort}");
        await console.Output.WriteLineAsync($"Credential manager API listening on 127.0.0.1:{CredentialManagerPort}");

        await easytierServer.WaitForExitAsync();
    }
}
