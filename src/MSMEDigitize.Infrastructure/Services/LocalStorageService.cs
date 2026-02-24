using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Infrastructure.Services;

/// <summary>Saves files to local disk (dev/fallback). 
/// Replace with AzureBlobStorageService or S3StorageService for production.</summary>
public class LocalStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IConfiguration config, ILogger<LocalStorageService> logger)
    {
        _logger = logger;
        _basePath = config["Storage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _baseUrl  = config["Storage:BaseUrl"]   ?? "/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(byte[] data, string fileName, string contentType)
    {
        var safe = Path.GetFileName(fileName);
        var folder = Path.Combine(_basePath, DateTime.UtcNow.ToString("yyyy/MM"));
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, $"{Guid.NewGuid():N}_{safe}");
        await File.WriteAllBytesAsync(fullPath, data);
        var rel = Path.GetRelativePath(_basePath, fullPath).Replace('\\', '/');
        _logger.LogDebug("Stored file {Name} → {Path}", fileName, fullPath);
        return $"{_baseUrl}/{rel}";
    }

    public async Task<byte[]> DownloadAsync(string url)
    {
        var rel  = url.StartsWith(_baseUrl) ? url[_baseUrl.Length..].TrimStart('/') : url.TrimStart('/');
        var path = Path.Combine(_basePath, rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) throw new FileNotFoundException($"File not found: {path}");
        return await File.ReadAllBytesAsync(path);
    }

    public Task DeleteAsync(string url)
    {
        var rel  = url.StartsWith(_baseUrl) ? url[_baseUrl.Length..].TrimStart('/') : url.TrimStart('/');
        var path = Path.Combine(_basePath, rel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
