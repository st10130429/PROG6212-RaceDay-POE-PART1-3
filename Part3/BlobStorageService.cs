using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>API-only Azure Blob Storage integration. MVC never connects to Azure directly.</summary>
public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);
}

public sealed class BlobStorageService : IBlobStorageService
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0 || file.Length > MaxBytes)
            throw new ArgumentException("Image must be between 1 byte and 5 MB.");
        if (!AllowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Only JPG, PNG or WEBP images are allowed.");

        var connectionString = _configuration["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlob__ConnectionString before uploading images.");

        var containerName = _configuration["AzureBlob:Container"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: ct);
        _logger.LogInformation("Uploaded image blob {BlobName}", blob.Name);
        return blob.Uri.ToString();
    }
}
