using PaymentService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PaymentService.Infrastructure.Locking;

/// <summary>
/// Distributed lock via Postgres session-level advisory locks (pg_advisory_lock/pg_advisory_unlock)
/// on a dedicated connection, rather than reaching for Redis - PaymentService already has a
/// Postgres connection and doesn't otherwise depend on Redis, so this needed no new infrastructure.
/// Session-scoped (not the transaction-scoped pg_advisory_xact_lock) on purpose: the critical
/// section this guards spans the Stripe HTTP call, which must not be done inside an open EF
/// transaction (that would hold a pooled connection - and block every other request sharing that
/// pool - for as long as Stripe takes to respond). A dedicated connection lets the lock be held
/// for that whole span independently of whatever EF is doing on its own connections.
///
/// Advisory lock keys are two int32s here, not one int64/bigint, purely because that's the
/// pg_advisory_lock overload used - both are equally valid Postgres APIs; this just avoids a
/// widening cast from the Guid bytes.
/// </summary>
public class PostgresAdvisoryOrderChargeLock : IOrderChargeLock
{
    private readonly string _connectionString;

    public PostgresAdvisoryOrderChargeLock(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PaymentDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:PaymentDatabase is not configured.");
    }

    public async Task<IAsyncDisposable> AcquireAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var (key1, key2) = ToLockKeys(orderId);

        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            // Blocks (this connection only - other queries elsewhere are unaffected) until no
            // other session holds the same key pair. Cancelling `cancellationToken` sends Postgres
            // a query-cancel while blocked here, which is the only bound on the wait - there's no
            // separate acquire-timeout.
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

    /// <summary>Folds the Guid's 16 bytes into two int32s - just enough bits of the OrderId to key
    /// the lock by, not a general-purpose hash. Collisions (two different OrderIds landing on the
    /// same key pair) would only ever cause unrelated orders to briefly serialize against each
    /// other, never a correctness problem - the existing-payment check inside the lock is what
    /// actually keys on the real OrderId.</summary>
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
                // Closing the connection is the backstop even if the explicit unlock above never
                // ran (e.g. the process crashed) - Postgres releases every session-level advisory
                // lock automatically when the owning connection/session ends, so there's no
                // permanent-orphan risk the way an unbounded Redis lock without a TTL would have.
                await _connection.DisposeAsync();
            }
        }
    }
}
