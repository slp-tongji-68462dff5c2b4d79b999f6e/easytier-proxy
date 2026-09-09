using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using EasytierProxy.Client.Easytier;
using EasytierProxy.Client.Proxy;

namespace EasytierProxy.Client.Commands;

[Command("connect")]
public sealed partial class ConnectCommand : ICommand
{
    [CommandOption("id")]
    public required string Id { get; set; }

    [CommandOption("credential", EnvironmentVariable = "EASIER_PROXY_CREDENTIAL")]
    public required string Credential { get; set; }

    [CommandOption("port")]
    public required int Port { get; set; }

    [CommandOption("easytier-core-command")]
    public string EasytierCoreCommand { get; set; } = "easytier-core";

    [CommandOption("easytier-network-name")]
    public required string EasytierNetworkName { get; set; }

    [CommandOption("easytier-peer")]
    public required IReadOnlyList<string> EasytierPeers { get; set; }

    [CommandOption("easytier-socks5-port")]
    public required int EasytierSocks5Port { get; set; }

    [CommandOption("easytier-http-port")]
    public required int EasytierHttpPort { get; set; }

    [CommandOption("easytier-rpc-port")]
    public required int EasytierRpcPort { get; set; }

    [CommandOption("proxy-host")]
    public required string ProxyHost { get; set; }

    [CommandOption("proxy-port")]
    public required int ProxyPort { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var credentialParts = this.Credential.Split(':', 2);
        var credentialSecret = credentialParts[0];
        var proxyPassword = credentialParts[1];

        var easytier = EasytierClient.Start(
            EasytierCoreCommand,
            EasytierNetworkName,
            credentialSecret,
            EasytierPeers,
            EasytierSocks5Port,
            EasytierRpcPort,
            console.RegisterCancellationHandler());

        using var proxy = ClientProxyServer.Create(
            Port,
            EasytierHttpPort,
            EasytierSocks5Port,
            ProxyHost,
            ProxyPort,
            Id,
            proxyPassword);

        await easytier.WaitForExitAsync();
    }
}
