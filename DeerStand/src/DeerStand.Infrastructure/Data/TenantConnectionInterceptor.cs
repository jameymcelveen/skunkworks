using System.Data.Common;
using DeerStand.Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DeerStand.Infrastructure.Data;

/// <summary>
/// Sets Postgres session GUCs used by RLS policies before each command.
/// <c>app.current_profile_id</c> is the Zitadel subject; empty string when unset.
/// </summary>
public sealed class TenantConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public const string ProfileIdSetting = "app.current_profile_id";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyTenant(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantAsync(connection, cancellationToken).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    private void ApplyTenant(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT set_config('{ProfileIdSetting}', @profileId, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "profileId";
        parameter.Value = tenantContext.ProfileId ?? string.Empty;
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();
    }

    private async Task ApplyTenantAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT set_config('{ProfileIdSetting}', @profileId, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "profileId";
        parameter.Value = tenantContext.ProfileId ?? string.Empty;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
