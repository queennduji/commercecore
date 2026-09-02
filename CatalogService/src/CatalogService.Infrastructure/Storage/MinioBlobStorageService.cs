using CatalogService.Application.Interfaces;
using CatalogService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CatalogService.Infrastructure.Storage;

public class MinioBlobStorageService : IBlobStorageService, IDisposable
{
    private readonly IMinioClient _minioClient;
    private readonly IMinioClient _presignClient;
    private readonly MinioOptions _options;

    public MinioBlobStorageService(IMinioClient minioClient, IOptions<MinioOptions> options)
    {
        _minioClient = minioClient;
        _options = options.Value;

        // Presigned URLs are signed locally (no network round-trip) but must be built against the
        // endpoint external clients can actually reach – "minio:9000" only resolves inside Docker.
        _presignClient = new MinioClient()
            .WithEndpoint(_options.PublicBaseUrl)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.PublicUseSSL)
            .Build();
    }

    public async Task<PresignedUploadUrl> CreatePresignedUploadUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken = default)
    {
        var args = new PresignedPutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithExpiry(_options.PresignedUploadExpirySeconds);

        var uploadUrl = await _presignClient.PresignedPutObjectAsync(args);
        var expiresAt = DateTime.UtcNow.AddSeconds(_options.PresignedUploadExpirySeconds);

        return new PresignedUploadUrl(uploadUrl, objectKey, GetPublicUrl(objectKey), expiresAt);
    }

    public string GetPublicUrl(string objectKey)
    {
        var scheme = _options.PublicUseSSL ? "https" : "http";
        return $"{scheme}://{_options.PublicBaseUrl}/{_options.BucketName}/{objectKey}";
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey);

        await _minioClient.RemoveObjectAsync(args, cancellationToken);
    }

    public void Dispose()
    {
        _presignClient.Dispose();
    }
}
