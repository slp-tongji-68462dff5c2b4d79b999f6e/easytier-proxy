using System.Text.Json.Serialization;

namespace EasytierProxy.Server.Credentials;

public sealed record CredentialListResult(
    [property: JsonPropertyName("credentials")] List<CredentialInfo>? Credentials);
