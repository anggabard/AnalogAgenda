namespace AnalogAgenda.Server;

/// <summary>Request body size limits (bytes). Kestrel ceiling must be &gt;= largest per-action limit.</summary>
public static class RequestBodySizeLimits
{
    /// <summary>Global MVC filter default (JSON APIs, including base64 images on notes, sessions, thumbnails).</summary>
    public const long Default = 70_000_000; // 70 MB

    /// <summary>Film photo upload (PhotoCreateDto); explicit on PhotoController.UploadPhoto.</summary>
    public const long PhotoUpload = 200_000_000; // 200 MB
}
