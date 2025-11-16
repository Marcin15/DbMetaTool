using FirebirdSql.Data.FirebirdClient;
using System.Text;

namespace DbMetaTool.UseCases.ExportDatabase.ExportDatabaseStrategies;

internal sealed class ExportProceduresStrategy : IExportDatabaseStrategy
{
    public void ExportDatabase(FbConnection connection, string outputDirectory)
    {
        var outputFileName = "3.procedures.sql";

        Directory.CreateDirectory(outputDirectory);

        var sb = new StringBuilder();

        AppendSetTermStart(sb);

        var procedures = GetProcedures(connection);

        foreach (var proc in procedures)
        {
            AppendCreateProcedure(sb, connection, proc);
        }

        foreach (var proc in procedures)
        {
            AppendAlterProcedure(sb, connection, proc);
        }

        AppendSetTermEnd(sb);

        File.WriteAllText(Path.Combine(outputDirectory, outputFileName), sb.ToString());
    }

    private static void AppendSetTermStart(StringBuilder sb)
        => sb.AppendLine("SET TERM ^ ;\n");

    private static void AppendSetTermEnd(StringBuilder sb)
        => sb.AppendLine("SET TERM ; ^");

    private static List<string> GetProcedures(FbConnection connection)
    {
        var list = new List<string>();
        var sqlProcs = @"SELECT RDB$PROCEDURE_NAME 
                        FROM RDB$PROCEDURES
                        WHERE RDB$SYSTEM_FLAG = 0
                        ORDER BY RDB$PROCEDURE_NAME";

        using var cmd = new FbCommand(sqlProcs, connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0).Trim());
        }
        return list;
    }

    private static void AppendCreateProcedure(StringBuilder sb, FbConnection connection, string procName)
    {
        var inParams = GetProcedureParameters(connection, procName, inputOnly: true);

        sb.AppendLine($"CREATE PROCEDURE {procName} ({string.Join(", ", inParams)})");
        sb.AppendLine("AS");
        sb.AppendLine("BEGIN");
        sb.AppendLine("  EXIT;");
        sb.AppendLine("END^");
        sb.AppendLine();
    }

    private static void AppendAlterProcedure(StringBuilder sb, FbConnection connection, string procName)
    {
        var procSource = GetProcedureSource(connection, procName);
        if (string.IsNullOrWhiteSpace(procSource)) return;

        var inParams = GetProcedureParameters(connection, procName, inputOnly: true);

        sb.AppendLine($"ALTER PROCEDURE {procName} ({string.Join(", ", inParams)})");
        sb.AppendLine("AS");
        sb.AppendLine(procSource);
        sb.AppendLine("END^\n");
    }

    private static List<string> GetProcedureParameters(FbConnection connection, string procName, bool inputOnly)
    {
        var parameters = new List<string>();

        var sqlParams = @"SELECT RDB$PARAMETER_NAME, RDB$FIELD_SOURCE, RDB$PARAMETER_TYPE
                         FROM RDB$PROCEDURE_PARAMETERS
                         WHERE RDB$PROCEDURE_NAME = @proc";

        if (inputOnly)
            sqlParams += " AND RDB$PARAMETER_TYPE = 0";

        sqlParams += " ORDER BY RDB$PARAMETER_NUMBER";

        using var cmd = new FbCommand(sqlParams, connection);
        cmd.Parameters.AddWithValue("@proc", procName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var paramName = reader.GetString(0).Trim();
            var typeName = reader.GetString(1).Trim();
            parameters.Add($"{paramName} {typeName}");
        }

        return parameters;
    }

    private static string GetProcedureSource(FbConnection connection, string procName)
    {
        var sql = @"SELECT RDB$PROCEDURE_SOURCE 
                   FROM RDB$PROCEDURES
                   WHERE RDB$PROCEDURE_NAME = @proc";

        using var cmd = new FbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@proc", procName);

        var result = cmd.ExecuteScalar();
        return result == DBNull.Value ? string.Empty : result.ToString()!.Trim();
    }
}
