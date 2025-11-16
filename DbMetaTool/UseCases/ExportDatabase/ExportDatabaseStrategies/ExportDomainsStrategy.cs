using FirebirdSql.Data.FirebirdClient;
using System.Text;

namespace DbMetaTool.UseCases.ExportDatabase.ExportDatabaseStrategies;

internal sealed class ExportDomainsStrategy : IExportDatabaseStrategy
{
    public void ExportDatabase(FbConnection connection, string outputDirectory)
    {
        var outputFileName = "1.domains.sql";

        var sql = @"SELECT RDB$FIELD_NAME, RDB$VALIDATION_SOURCE FROM RDB$FIELDS WHERE RDB$FIELD_NAME NOT LIKE 'RDB$%'";
        using var cmd = new FbCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        var sb = new StringBuilder();

        while (reader.Read())
        {
            var name = reader.GetString(0).Trim();
            sb.AppendLine($"CREATE DOMAIN {name} AS VARCHAR(100);");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(outputDirectory, outputFileName), sb.ToString());
    }
}
