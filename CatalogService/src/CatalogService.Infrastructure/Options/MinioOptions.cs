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

    /// <summary>Whether `Endpoint` (the internal address) speaks TLS. MinIO itself has no TLS
    /// configured in this repo's compose files (local or prod) — this should stay false unless
    /// that changes. Deliberately separate from PublicUseSSL: the internal connection and the
    /// public-facing one can legitimately need different schemes (e.g. prod terminates TLS for
    /// PublicBaseUrl at an nginx reverse proxy in front of a plain-HTTP MinIO container).</summary>
    public bool UseSSL { get; set; }

    /// <summary>Whether `PublicBaseUrl` (the address embedded in product image URLs and used by
    /// the presign client) speaks TLS. False for local dev (bare "localhost:9000"); true in prod
    /// once PublicBaseUrl is a real HTTPS-terminating hostname.</summary>
    public bool PublicUseSSL { get; set; }

    public int PresignedUploadExpirySeconds { get; set; } = 300;
}
