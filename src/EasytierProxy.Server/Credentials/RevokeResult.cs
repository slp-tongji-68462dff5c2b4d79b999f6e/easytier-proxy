using System.Text.Json.Serialization;

namespace EasytierProxy.Server.Credentials;

public sealed record RevokeResult(
    [property: JsonPropertyName("success")] bool Success);
