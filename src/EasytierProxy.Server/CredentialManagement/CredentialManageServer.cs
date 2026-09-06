using EasytierProxy.Server.Credentials;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EasytierProxy.Server.CredentialManagement;

public sealed class CredentialManageServer : IAsyncDisposable
{
    private readonly EasytierCredentialManager credentials;
    private readonly ProxyUserRegistry users;
    private readonly WebApplication app;

    private CredentialManageServer(
        EasytierCredentialManager credentials, ProxyUserRegistry users, WebApplication app)
    {
        this.credentials = credentials;
        this.users = users;
        this.app = app;
    }

    public static async Task<CredentialManageServer> Start(
        EasytierCredentialManager credentials, ProxyUserRegistry users, string listen)
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseUrls(listen);

        var app = builder.Build();

        var server = new CredentialManageServer(credentials, users, app);

        app.MapPost("/generate", server.HandleGenerateAsync);
        app.MapPost("/revoke", server.HandleRevokeAsync);
        app.MapPost("/list", server.HandleListAsync);

        await app.StartAsync();
        return server;
    }

    public async Task StopAsync()
    {
        await this.app.StopAsync();
    }

    private async Task<IResult> HandleGenerateAsync(GenerateRequest request, CancellationToken cancellationToken)
    {
        var credential = await this.credentials.GenerateAsync(request.TimeToLive, cancellationToken);

        var proxyPassword = ProxyUserRegistry.GeneratePassword();
        this.users.Add(credential.CredentialId, proxyPassword);

        return Results.Json(new
        {
            id = credential.CredentialId,
            credential = $"{credential.CredentialSecret}:{proxyPassword}",
        });
    }

    private async Task<IResult> HandleRevokeAsync(RevokeRequest request, CancellationToken cancellationToken)
    {
        await this.credentials.RevokeAsync(request.Id, cancellationToken);
        this.users.Remove(request.Id);
        return Results.NoContent();
    }

    private async Task<IResult> HandleListAsync(ListRequest request, CancellationToken cancellationToken)
    {
        var credentials = await this.credentials.ListAsync(cancellationToken);

        IEnumerable<CredentialInfo> selected = credentials;
        if (request.Ids is not null)
        {
            var byId = credentials.ToDictionary(c => c.CredentialId, StringComparer.Ordinal);
            selected = request.Ids
                .Where(id => byId.ContainsKey(id))
                .Select(id => byId[id]);
        }

        var result = selected.Select(credential => new
        {
            id = credential.CredentialId,
            expire = DateTimeOffset.FromUnixTimeSeconds(credential.ExpiryUnix),
        });
        return Results.Json(result);
    }

    public async ValueTask DisposeAsync()
    {
        await this.app.DisposeAsync();
    }
}
