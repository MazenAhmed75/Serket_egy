namespace ECommerceStore.Web.Services;

/// <summary>The one place that decides which uploaded images are accepted (product photos, gallery photos, receipts).</summary>
public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    /// <summary>Returns a customer-friendly message when the file is not acceptable, or null when it is fine.</summary>
    public static string? Validate(IFormFile file)
    {
        if (file.Length > MaxBytes)
        {
            return $"\"{file.FileName}\" is larger than 5 MB.";
        }

        // The content type is sent by the browser and can be faked, so the first bytes of the file are checked too.
        if (!AllowedContentTypes.Contains(file.ContentType) || !LooksLikeImage(file))
        {
            return $"\"{file.FileName}\" must be a JPEG, PNG or WEBP image.";
        }

        return null;
    }

    private static bool LooksLikeImage(IFormFile file)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        if (read < header.Length)
        {
            return false;
        }

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isWebp = header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8);

        return isJpeg || isPng || isWebp;
    }
}
