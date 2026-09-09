using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace EasytierProxy.Client.Proxy;

public sealed class ClientProxyServer : IDisposable
{
    private readonly ProxyServer hopA;
    private readonly ProxyServer hopB;

    private ClientProxyServer(ProxyServer hopA, ProxyServer hopB)
    {
        this.hopA = hopA;
        this.hopB = hopB;
    }

    public static ClientProxyServer Create(
        int httpProxyPort,
        int internalHopAPort,
        int socks5Port,
        string proxyHost,
        int proxyPort,
        string credentialId,
        string proxyPassword)
    {
        var hopA = new ProxyServer(
            userTrustRootCertificate: false,
            machineTrustRootCertificate: false,
            trustRootCertificateAsAdmin: false)
        {
            UpStreamHttpProxy = new ExternalProxy("127.0.0.1", socks5Port)
            {
                ProxyType = ExternalProxyType.Socks5,
                ProxyDnsRequests = true,
            },
        };

        hopA.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, internalHopAPort, decryptSsl: false));
        hopA.Start(changeSystemProxySettings: false);

        var hopB = new ProxyServer(
            userTrustRootCertificate: false,
            machineTrustRootCertificate: false,
            trustRootCertificateAsAdmin: false)
        {
            UpStreamHttpProxy = new ExternalProxy("127.0.0.1", internalHopAPort)
            {
                ProxyType = ExternalProxyType.Http,
                NextHop = new ExternalProxy(proxyHost, proxyPort, credentialId, proxyPassword)
                {
                    ProxyType = ExternalProxyType.Http,
                },
            },
        };

        hopB.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, httpProxyPort, decryptSsl: false));
        hopB.Start(changeSystemProxySettings: false);

        return new ClientProxyServer(hopA, hopB);
    }

    public void Dispose()
    {
        this.hopB.Dispose();
        this.hopA.Dispose();
    }
}
