using EasytierProxy.Server.Easytier;
using EasytierProxy.Server.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Tjslp.CredentialManager.Protocol;

namespace EasytierProxy.Server.CredentialManagement;

public sealed class CredentialManageServer : IAsyncDisposable
{
    private readonly EasytierCredentialManager credentials;
    private readonly ProxyCredentialManager proxyCredentials;
    private readonly WebApplication app;

    private CredentialManageServer(
        EasytierCredentialManager credentials, ProxyCredentialManager proxyCredentials, WebApplication app)
    {
        this.credentials = credentials;
        this.proxyCredentials = proxyCredentials;
        this.app = app;
    }

    public static async Task<CredentialManageServer> StartAsync(
        EasytierCredentialManager credentials, ProxyCredentialManager proxyCredentials, int port)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

        builder.Services.AddRouting();
        builder.WebHost.UseKestrel(kestrel => kestrel.ListenLocalhost(port));

        var app = builder.Build();

        var server = new CredentialManageServer(credentials, proxyCredentials, app);

        app.MapPost("/create", server.HandleCreateAsync);
        app.MapPost("/query", server.HandleQueryAsync);
        app.MapPost("/revoke", server.HandleRevokeAsync);

        await app.StartAsync();
        return server;
    }

    private async Task<CreateResponse> HandleCreateAsync(CreateRequest request, CancellationToken cancellationToken)
    {
        var credential = await this.credentials.GenerateAsync(request.Expire, cancellationToken);

        var proxyPassword = await this.proxyCredentials.AddAsync(credential.CredentialId, cancellationToken);

        return new CreateResponse(
            credential.CredentialId,
            $"{credential.CredentialSecret}:{proxyPassword}",
            credential.Expiry);
    }

    private async Task<QueryResponse> HandleQueryAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        var byId = await this.credentials.ListAsync(cancellationToken)
            .ToDictionaryAsync();

        var items = new List<QueryItem>();
        foreach (var credentialId in request.CredentialIds)
        {
            if (byId.TryGetValue(credentialId, out var expiry))
            {
                items.Add(new QueryItem(credentialId, expiry));
            }
            else
            {
                await this.proxyCredentials.RemoveAsync(credentialId, cancellationToken);
            }
        }

        return new QueryResponse(items);
    }

    private async Task<RevokeResponse> HandleRevokeAsync(RevokeRequest request, CancellationToken cancellationToken)
    {
        var succeeded = await this.credentials.RevokeAsync(request.CredentialId, cancellationToken);
        if (succeeded)
        {
            await this.proxyCredentials.RemoveAsync(request.CredentialId, cancellationToken);
        }

        return new RevokeResponse(succeeded);
    }

    public async ValueTask DisposeAsync()
    {
        await this.app.DisposeAsync();
    }
}
