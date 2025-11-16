using FirebirdSql.Data.FirebirdClient;
using System.Text;

namespace DbMetaTool.UseCases.ExportDatabase.ExportDatabaseStrategies;

internal sealed class ExportTablesStrategy : IExportDatabaseStrategy
{
    public void ExportDatabase(FbConnection connection, string outputDirectory)
    {
        var outputFileName = "2.tables.sql";

        Directory.CreateDirectory(outputDirectory);

        var sb = new StringBuilder();
        var tables = GetUserTables(connection);

        foreach (var table in tables)
        {
            AppendCreateTable(sb, connection, table);
            AppendPrimaryKey(sb, connection, table);
            sb.AppendLine();
        }

        AppendForeignKeys(sb, connection);

        File.WriteAllText(Path.Combine(outputDirectory, outputFileName), sb.ToString());
    }


    private static IEnumerable<string> GetUserTables(FbConnection con)
    {
        var sql = @"SELECT RDB$RELATION_NAME 
                   FROM RDB$RELATIONS 
                   WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL
                   ORDER BY RDB$RELATION_NAME";

        using var cmd = new FbCommand(sql, con);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            yield return reader.GetString(0).Trim();
        }
    }

    private static void AppendCreateTable(StringBuilder sb, FbConnection con, string tableName)
    {
        sb.AppendLine($"CREATE TABLE {tableName} (");

        var columns = GetTableColumns(con, tableName);
        sb.AppendLine(string.Join(",\n", columns.Select(c => "    " + c)));

        sb.AppendLine(");\n");
    }

    private static List<string> GetTableColumns(FbConnection con, string tableName)
    {
        var sql = @"
            SELECT rf.RDB$FIELD_NAME AS COL_NAME,
                   f.RDB$FIELD_NAME AS DOMAIN_NAME,
                   rf.RDB$NULL_FLAG
            FROM RDB$RELATION_FIELDS rf
            JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            WHERE rf.RDB$RELATION_NAME = @tbl
            ORDER BY rf.RDB$FIELD_POSITION";

        using var cmd = new FbCommand(sql, con);
        cmd.Parameters.AddWithValue("@tbl", tableName);

        using var reader = cmd.ExecuteReader();

        var cols = new List<string>();

        while (reader.Read())
        {
            var colName = reader.GetString(0).Trim();
            var domainName = reader.GetString(1).Trim();
            bool notNull = !reader.IsDBNull(2) && reader.GetInt16(2) == 1;

            cols.Add($"{colName} {domainName}{(notNull ? " NOT NULL" : "")}");
        }

        return cols;
    }

    private static void AppendPrimaryKey(StringBuilder sb, FbConnection con, string tableName)
    {
        const string sql = @"
            SELECT s.RDB$FIELD_NAME
            FROM RDB$RELATION_CONSTRAINTS rc
            JOIN RDB$INDEX_SEGMENTS s ON rc.RDB$INDEX_NAME = s.RDB$INDEX_NAME
            WHERE rc.RDB$RELATION_NAME = @tbl AND rc.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
            ORDER BY s.RDB$FIELD_POSITION";

        using var cmd = new FbCommand(sql, con);
        cmd.Parameters.AddWithValue("@tbl", tableName);
        using var reader = cmd.ExecuteReader();

        var pkCols = new List<string>();
        while (reader.Read()) pkCols.Add(reader.GetString(0).Trim());

        if (pkCols.Count > 0)
            sb.AppendLine($"ALTER TABLE {tableName} ADD PRIMARY KEY ({string.Join(", ", pkCols)});");
    }

    private static void AppendForeignKeys(StringBuilder sb, FbConnection con)
    {
        var sql = @"
            SELECT rc.RDB$CONSTRAINT_NAME,
                   r1.RDB$RELATION_NAME AS TABLE_FROM,
                   s1.RDB$FIELD_NAME AS COL_FROM,
                   r2.RDB$RELATION_NAME AS TABLE_TO,
                   s2.RDB$FIELD_NAME AS COL_TO
            FROM RDB$REF_CONSTRAINTS rc
            JOIN RDB$RELATION_CONSTRAINTS r1 ON rc.RDB$CONSTRAINT_NAME = r1.RDB$CONSTRAINT_NAME
            JOIN RDB$RELATION_CONSTRAINTS r2 ON rc.RDB$CONST_NAME_UQ = r2.RDB$CONSTRAINT_NAME
            JOIN RDB$INDEX_SEGMENTS s1 ON r1.RDB$INDEX_NAME = s1.RDB$INDEX_NAME
            JOIN RDB$INDEX_SEGMENTS s2 ON r2.RDB$INDEX_NAME = s2.RDB$INDEX_NAME";

        using var cmd = new FbCommand(sql, con);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var tableFrom = reader.GetString(1).Trim();
            var colFrom = reader.GetString(2).Trim();
            var tableTo = reader.GetString(3).Trim();
            var colTo = reader.GetString(4).Trim();

            sb.AppendLine(
                $"ALTER TABLE {tableFrom} ADD CONSTRAINT FK_{tableFrom}_{colFrom} " +
                $"FOREIGN KEY ({colFrom}) REFERENCES {tableTo} ({colTo});"
            );
        }
    }
}
