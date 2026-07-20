namespace CatalogService.Infrastructure.Options;

public class MinioOptions
{
    public const string SectionName = "Minio";

    /// <summary>Host:port CatalogService itself uses to reach MinIO (e.g. "minio:9000" inside Docker).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Host:port external clients use to reach MinIO for presigned uploads and viewing images (e.g. "localhost:9000").</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "catalog-product-images";
    public bool UseSSL { get; set; }
    public int PresignedUploadExpirySeconds { get; set; } = 300;
}
