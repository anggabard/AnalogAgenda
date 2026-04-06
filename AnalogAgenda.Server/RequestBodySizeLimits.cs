namespace AnalogAgenda.Server;

/// <summary>Request body size limits (bytes). Kestrel ceiling must be &gt;= largest per-action limit.</summary>
public static class RequestBodySizeLimits
{
    /// <summary>Default max JSON body for most API actions.</summary>
    public const long Default = 30_000_000; // 30 MB

    /// <summary>Photo upload (base64 JSON).</summary>
    public const long PhotoUpload = 200_000_000; // 200 MB
}
