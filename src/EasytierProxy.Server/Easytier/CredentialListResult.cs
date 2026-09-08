using System.Text.Json.Serialization;

namespace EasytierProxy.Server.Easytier;

public sealed record CredentialListResult(
    [property: JsonPropertyName("credentials")] List<CredentialInfo> Credentials);
