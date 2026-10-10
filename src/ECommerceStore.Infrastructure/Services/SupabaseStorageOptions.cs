namespace ECommerceStore.Infrastructure.Services;

/// <summary>
/// Settings for Supabase Storage (configuration section "Supabase"; environment variables Supabase__Url, Supabase__ServiceKey ...).
/// The service key is a server-side secret: it must only ever live in the host's environment variables, never in the repository.
/// </summary>
public sealed class SupabaseStorageOptions
{
    /// <summary>The project address, e.g. https://abcdefgh.supabase.co</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The project's secret key (the "service_role" key or a "secret" API key). Never sent to browsers.</summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary>A PUBLIC bucket for product photos.</summary>
    public string PublicBucket { get; set; } = "product-images";

    /// <summary>A PRIVATE bucket for payment receipts. It must not be public.</summary>
    public string PrivateBucket { get; set; } = "receipts";

    /// <summary>Returns a message describing what is wrong, or null when the settings are usable.</summary>
    public string? Validate()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return "Supabase:Url must be the project's https address, for example https://abcdefgh.supabase.co.";
        }

        if (string.IsNullOrWhiteSpace(ServiceKey))
        {
            return "Supabase:ServiceKey is missing (set the environment variable Supabase__ServiceKey).";
        }

        if (string.IsNullOrWhiteSpace(PublicBucket) || string.IsNullOrWhiteSpace(PrivateBucket) ||
            string.Equals(PublicBucket, PrivateBucket, StringComparison.OrdinalIgnoreCase))
        {
            return "Supabase:PublicBucket and Supabase:PrivateBucket must both be set and must be two different buckets.";
        }

        return null;
    }
}
