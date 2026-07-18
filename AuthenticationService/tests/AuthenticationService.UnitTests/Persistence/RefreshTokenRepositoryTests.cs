using AuthenticationService.Domain.Entities;
using AuthenticationService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.UnitTests.Persistence;

public class RefreshTokenRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AuthDbContext _dbContext = null!;
    private RefreshTokenRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AuthDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new RefreshTokenRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetByTokenAsync_ReturnsTheSameToken()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "lookup-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _repository.AddAsync(token);
        await _repository.SaveChangesAsync();

        var found = await _repository.GetByTokenAsync("lookup-token");

        Assert.NotNull(found);
        Assert.Equal(token.Id, found!.Id);
    }

    [Fact]
    public async Task GetByTokenAsync_UnknownToken_ReturnsNull()
    {
        var found = await _repository.GetByTokenAsync("does-not-exist");

        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteInactiveOlderThanAsync_RemovesExpiredAndRevokedTokens_KeepsActiveOnes()
    {
        var cutoff = DateTime.UtcNow;

        var expired = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Token = "expired", CreatedAt = cutoff.AddDays(-10), ExpiresAt = cutoff.AddDays(-1) };
        var revoked = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Token = "revoked", CreatedAt = cutoff.AddDays(-10), ExpiresAt = cutoff.AddDays(5), RevokedAt = cutoff.AddDays(-1) };
        var active = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Token = "active", CreatedAt = cutoff.AddDays(-1), ExpiresAt = cutoff.AddDays(5) };

        await _repository.AddAsync(expired);
        await _repository.AddAsync(revoked);
        await _repository.AddAsync(active);
        await _repository.SaveChangesAsync();

        var deletedCount = await _repository.DeleteInactiveOlderThanAsync(cutoff);

        Assert.Equal(2, deletedCount);
        Assert.Null(await _repository.GetByTokenAsync("expired"));
        Assert.Null(await _repository.GetByTokenAsync("revoked"));
        Assert.NotNull(await _repository.GetByTokenAsync("active"));
    }
}
