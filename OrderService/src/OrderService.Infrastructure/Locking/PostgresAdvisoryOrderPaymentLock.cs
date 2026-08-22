using OrderService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OrderService.Infrastructure.Locking;

/// <summary>
/// Same mechanism as PaymentService's PostgresAdvisoryOrderChargeLock - session-level Postgres
/// advisory locks (pg_advisory_lock/pg_advisory_unlock) on a dedicated connection. Session-scoped
/// rather than transaction-scoped because the critical section this guards spans the HTTP call to
/// PaymentService, which must not happen inside an open EF transaction (that would hold a pooled
/// connection - blocking every other request sharing that pool - for as long as the call takes).
/// </summary>
public class PostgresAdvisoryOrderPaymentLock : IOrderPaymentLock
{
    private readonly string _connectionString;

    public PostgresAdvisoryOrderPaymentLock(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OrderDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:OrderDatabase is not configured.");
    }

    public async Task<IAsyncDisposable> AcquireAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var (key1, key2) = ToLockKeys(orderId);

        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using var acquireCommand = new NpgsqlCommand("SELECT pg_advisory_lock(@key1, @key2)", connection);
            acquireCommand.Parameters.AddWithValue("key1", key1);
            acquireCommand.Parameters.AddWithValue("key2", key2);
            await acquireCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        return new LockHandle(connection, key1, key2);
    }

    /// <summary>Same OrderId-to-keys folding as PaymentService's lock - see that type's comment.
    /// Deliberately a different advisory-lock key *space* than PaymentService's (separate Postgres
    /// instances/connections entirely), so no risk of these two services' locks colliding with
    /// each other even though they derive keys from the same OrderId values.</summary>
    private static (int Key1, int Key2) ToLockKeys(Guid orderId)
    {
        Span<byte> bytes = stackalloc byte[16];
        orderId.TryWriteBytes(bytes);
        return (BitConverter.ToInt32(bytes[..4]), BitConverter.ToInt32(bytes[8..12]));
    }

    private class LockHandle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly int _key1;
        private readonly int _key2;

        public LockHandle(NpgsqlConnection connection, int key1, int key2)
        {
            _connection = connection;
            _key1 = key1;
            _key2 = key2;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await using var releaseCommand = new NpgsqlCommand("SELECT pg_advisory_unlock(@key1, @key2)", _connection);
                    releaseCommand.Parameters.AddWithValue("key1", _key1);
                    releaseCommand.Parameters.AddWithValue("key2", _key2);
                    await releaseCommand.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
