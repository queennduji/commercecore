namespace CatalogService.Application.Interfaces;

public record PresignedUploadUrl(string UploadUrl, string ObjectKey, string PublicUrl, DateTime ExpiresAt);

public interface IBlobStorageService
{
    Task<PresignedUploadUrl> CreatePresignedUploadUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken = default);

    string GetPublicUrl(string objectKey);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
