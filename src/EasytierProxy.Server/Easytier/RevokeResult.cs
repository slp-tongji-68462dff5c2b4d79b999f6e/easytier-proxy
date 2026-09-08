using System.Text.Json.Serialization;

namespace EasytierProxy.Server.Easytier;

public sealed record RevokeResult(
    [property: JsonPropertyName("success")] bool Success);
