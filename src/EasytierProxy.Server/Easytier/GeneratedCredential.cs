using System.Text.Json.Serialization;

namespace EasytierProxy.Server.Easytier;

public sealed record GeneratedCredential(
    [property: JsonPropertyName("credential_id")] string CredentialId,
    [property: JsonPropertyName("credential_secret")] string CredentialSecret);
