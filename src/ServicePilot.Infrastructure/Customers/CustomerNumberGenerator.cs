using System.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

using ServicePilot.Application.Customers;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Customers;

internal sealed class CustomerNumberGenerator(
    ServicePilotDbContext dbContext)
    : ICustomerNumberGenerator
{
    public async Task<long> NextAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty",
                nameof(organizationId));
        }

        NpgsqlConnection connection =
            (NpgsqlConnection)dbContext
                .Database
                .GetDbConnection();
        bool shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using NpgsqlCommand command =
                connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO customer_number_counters (
                    organization_id,
                    next_value
                )
                VALUES (@organizationId, 2)
                ON CONFLICT (organization_id)
                DO UPDATE
                    SET next_value =
                        customer_number_counters.next_value + 1
                RETURNING next_value - 1;
                """;
            command.Parameters.AddWithValue(
                "organizationId",
                organizationId);

            if (dbContext.Database.CurrentTransaction
                is IDbContextTransaction transaction)
            {
                command.Transaction =
                    (NpgsqlTransaction)transaction
                        .GetDbTransaction();
            }

            object? result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}