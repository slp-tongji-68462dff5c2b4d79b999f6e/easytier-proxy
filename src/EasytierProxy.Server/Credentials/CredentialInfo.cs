using System.Text.Json.Serialization;

namespace EasytierProxy.Server.Credentials;

public sealed record CredentialInfo(
    [property: JsonPropertyName("credential_id")] string CredentialId,
    [property: JsonPropertyName("groups")] List<string>? Groups,
    [property: JsonPropertyName("allow_relay")] bool AllowRelay,
    [property: JsonPropertyName("expiry_unix")] long ExpiryUnix,
    [property: JsonPropertyName("allowed_proxy_cidrs")] List<string>? AllowedProxyCidrs,
    [property: JsonPropertyName("reusable")] bool Reusable);
