using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ECommerceStore.Core.Exceptions;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;

namespace ECommerceStore.Infrastructure.Services;

/// <summary>
/// Stores files in Supabase Storage through its REST API (no extra packages). Product photos go to a public bucket and
/// are referenced by their public URL; payment receipts go to a private bucket, are referenced as "private:folder/file",
/// and are only ever read back by the server with the secret key. File names are generated, extensions are limited to
/// safe image types, and only files that match our own naming pattern are ever opened or deleted.
/// </summary>
public class SupabaseFileStorageService : IFileStorageService
{
    private readonly HttpClient _http;
    private readonly SupabaseStorageOptions _options;
    private readonly string _baseUrl;

    public SupabaseFileStorageService(HttpClient http, SupabaseStorageOptions options)
    {
        _http = http;
        _options = options;
        _baseUrl = options.Url.TrimEnd('/');
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var fileName = StoredFileNames.NewFileName(originalFileName);
        var objectPath = $"{subFolder}/{fileName}";

        // The name is unique, so browsers and the CDN may keep the photo for a year.
        await UploadAsync(_options.PublicBucket, objectPath, content, fileName, "max-age=31536000", cancellationToken);
        return PublicUrl(objectPath);
    }

    public async Task<string> SavePrivateAsync(Stream content, string originalFileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var fileName = StoredFileNames.NewFileName(originalFileName);
        await UploadAsync(_options.PrivateBucket, $"{subFolder}/{fileName}", content, fileName, null, cancellationToken);
        return $"{StoredFileNames.PrivatePrefix}{subFolder}/{fileName}";
    }

    public async Task<Stream?> OpenPrivateAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (!StoredFileNames.TryParsePrivate(reference, out var folder, out var file))
        {
            return null;
        }

        using var request = NewRequest(HttpMethod.Get, $"{_baseUrl}/storage/v1/object/authenticated/{_options.PrivateBucket}/{folder}/{file}");
        using var response = await SendAsync(request, cancellationToken);

        // Supabase answers "object not found" with 400 or 404 depending on the version.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return null;
        }

        EnsureSuccess(response, "read");

        // Receipts are small (5 MB at most), so they are read fully and the connection is released straight away.
        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public async Task DeleteAsync(string? reference, CancellationToken cancellationToken = default)
    {
        if (StoredFileNames.TryParsePrivate(reference, out var folder, out var file))
        {
            await DeleteObjectAsync(_options.PrivateBucket, $"{folder}/{file}", cancellationToken);
            return;
        }

        // A public photo is only ours when it is a file in our public bucket with a generated name.
        var publicBase = PublicUrl(string.Empty);
        if (reference is not null && reference.StartsWith(publicBase, StringComparison.Ordinal) &&
            StoredFileNames.TryParsePublicObject(reference[publicBase.Length..], out var publicFolder, out var publicFile))
        {
            await DeleteObjectAsync(_options.PublicBucket, $"{publicFolder}/{publicFile}", cancellationToken);
        }

        // Anything else (starter photos shipped with the site, old local paths, empty values) is not ours to delete.
    }

    private string PublicUrl(string objectPath) => $"{_baseUrl}/storage/v1/object/public/{_options.PublicBucket}/{objectPath}";

    private async Task UploadAsync(string bucket, string objectPath, Stream content, string fileName, string? cacheControl, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        using var request = NewRequest(HttpMethod.Post, $"{_baseUrl}/storage/v1/object/{bucket}/{objectPath}");
        var body = new ByteArrayContent(buffer.ToArray());
        body.Headers.ContentType = new MediaTypeHeaderValue(StoredFileNames.ContentTypeFor(fileName));
        request.Content = body;
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        if (cacheControl is not null)
        {
            request.Headers.TryAddWithoutValidation("cache-control", cacheControl);
        }

        using var response = await SendAsync(request, cancellationToken);
        EnsureSuccess(response, "save");
    }

    private async Task DeleteObjectAsync(string bucket, string objectPath, CancellationToken cancellationToken)
    {
        using var request = NewRequest(HttpMethod.Delete, $"{_baseUrl}/storage/v1/object/{bucket}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { prefixes = new[] { objectPath } }),
            Encoding.UTF8,
            "application/json");

        using var response = await SendAsync(request, cancellationToken);

        // Already gone is fine: the goal is that the file no longer exists.
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            EnsureSuccess(response, "delete");
        }
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", _options.ServiceKey);

        // The newer "sb_secret_..." keys are not JWTs and go only in the apikey header; the older service_role key is a JWT.
        if (!_options.ServiceKey.StartsWith("sb_", StringComparison.Ordinal))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceKey);
        }

        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Timeouts and network problems become one predictable exception (the message never contains the key).
            throw new FileStorageException("The file storage service could not be reached.", ex);
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new FileStorageException($"The file storage service refused to {action} the file (HTTP {(int)response.StatusCode}).");
        }
    }
}
