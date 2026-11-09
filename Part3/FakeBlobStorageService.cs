using Microsoft.AspNetCore.Http;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>Deterministic Blob Storage substitute for API integration tests.</summary>
public sealed class FakeBlobStorageService : IBlobStorageService
{
    public const string BannerUrl = "https://unit-test-storage.example/raceday-images/banners/test-image.jpg";
    public const string ProfileUrl = "https://unit-test-storage.example/raceday-images/profiles/test-image.jpg";

    public Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("Image must not be empty.");

        return Task.FromResult(folder == "banners" ? BannerUrl : ProfileUrl);
    }
}
