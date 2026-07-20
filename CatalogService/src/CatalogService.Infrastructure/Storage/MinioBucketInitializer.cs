using CatalogService.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CatalogService.Infrastructure.Storage;

public class MinioBucketInitializer : IHostedService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _options;

    public MinioBucketInitializer(IMinioClient minioClient, IOptions<MinioOptions> options)
    {
        _minioClient = minioClient;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!exists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.BucketName), cancellationToken);
        }

        var publicReadPolicy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [
                {
                  "Effect": "Allow",
                  "Principal": {"AWS": ["*"]},
                  "Action": ["s3:GetObject"],
                  "Resource": ["arn:aws:s3:::{{_options.BucketName}}/*"]
                }
              ]
            }
            """;

        await _minioClient.SetPolicyAsync(
            new SetPolicyArgs().WithBucket(_options.BucketName).WithPolicy(publicReadPolicy),
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
