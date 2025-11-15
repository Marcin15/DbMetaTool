using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DbMetaTool;

public static class Program
{
    // Przykładowe wywołania:
    // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
    // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
    // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Użycie:");
            Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
            Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
            Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
            return 1;
        }

        try
        {
            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "build-db":
                    {
                        string dbDir = GetArgValue(args, "--db-dir");
                        string scriptsDir = GetArgValue(args, "--scripts-dir");

                        BuildDatabase(dbDir, scriptsDir);
                        Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                        return 0;
                    }

                case "export-scripts":
                    {
                        string connStr = GetArgValue(args, "--connection-string");
                        string outputDir = GetArgValue(args, "--output-dir");

                        ExportScripts(connStr, outputDir);
                        Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                        return 0;
                    }

                case "update-db":
                    {
                        string connStr = GetArgValue(args, "--connection-string");
                        string scriptsDir = GetArgValue(args, "--scripts-dir");

                        UpdateDatabase(connStr, scriptsDir);
                        Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                        return 0;
                    }

                default:
                    Console.WriteLine($"Nieznane polecenie: {command}");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Błąd: " + ex.Message);
            return -1;
        }
    }

    private static string GetArgValue(string[] args, string name)
    {
        int idx = Array.IndexOf(args, name);
        if (idx == -1 || idx + 1 >= args.Length)
            throw new ArgumentException($"Brak wymaganego parametru {name}");
        return args[idx + 1];
    }

    /// <summary>
    /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
    /// </summary>
    public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
    {
        databaseDirectory = "F:\\AAA_TEST\\TEST_DATABASE.FDB";
        scriptsDirectory = "F:\\AAA_TEST\\scripts";
        //// TODO:
        //// 1) Utwórz pustą bazę danych FB 5.0 w katalogu databaseDirectory.
        //// 2) Wczytaj i wykonaj kolejno skrypty z katalogu scriptsDirectory
        ////    (tylko domeny, tabele, procedury).
        //// 3) Obsłuż błędy i wyświetl raport.
        //throw new NotImplementedException();

        Directory.CreateDirectory(databaseDirectory);
        string dbPath = Path.Combine(databaseDirectory, "database.fdb");

        string connectionString = new FbConnectionStringBuilder
        {
            Database = dbPath,
            UserID = "SYSDBA",
            Password = "masterkey",
            DataSource = "localhost"
        }.ToString();

        FbConnection.CreateDatabase(connectionString, overwrite: true);

        foreach (var file in Directory.GetFiles(scriptsDirectory).OrderBy(f => f))
        {
            string script = File.ReadAllText(file);
            ExecuteScript(connectionString, script);
        }
    }

    /// <summary>
    /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
    /// </summary>
    public static void ExportScripts(string connectionString, string outputDirectory)
    {
        // TODO:
        // 1) Połącz się z bazą danych przy użyciu connectionString.
        // 2) Pobierz metadane domen, tabel (z kolumnami) i procedur.
        // 3) Wygeneruj pliki .sql / .json / .txt w outputDirectory.

        connectionString = "initial catalog=F:\\AAA_TEST\\TEST_DATABASE.FDB\\database.fdb;user id=SYSDBA;password=masterkey;data source=localhost";
        outputDirectory = "F:\\AAA_TEST\\scripts\\new";

        Directory.CreateDirectory(outputDirectory);


        using var con = new FbConnection(connectionString);
        con.Open();

        ExportDomains(con, outputDirectory);
        ExportTables(con, outputDirectory);
        ExportProcedures(con, outputDirectory);
    }

    /// <summary>
    /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
    /// </summary>
    public static void UpdateDatabase(string connectionString, string scriptsDirectory)
    {
        // TODO:
        // 1) Połącz się z bazą danych przy użyciu connectionString.
        // 2) Wykonaj skrypty z katalogu scriptsDirectory (tylko obsługiwane elementy).
        // 3) Zadbaj o poprawną kolejność i bezpieczeństwo zmian.
        foreach (var file in Directory.GetFiles(scriptsDirectory).OrderBy(f => f))
        {
            string script = File.ReadAllText(file);
            ExecuteScript(connectionString, script);
        }
    }

    private static void ExecuteScript(string connectionString, string script)
    {
        using var con = new FbConnection(connectionString);
        con.Open();

        if (script.Contains("SET TERM"))
        {
            ExeciteStoredProcedure(con, script);
            return;
        }

        foreach (string part in script.Split(';'))
        {
            string trimmed = part.Trim();
            if (trimmed.Length == 0) continue;

            using var cmd = new FbCommand(trimmed, con);
            cmd.ExecuteNonQuery();
        }
    }

    private static void ExeciteStoredProcedure(FbConnection connection, string script)
    {
        var storedProcedures = SplitStoredProcedures(script);
        foreach (var storedProcedure in storedProcedures)
        {
            var trimmed = storedProcedure.Trim().Replace("^", string.Empty);
            using var cmd = new FbCommand(trimmed, connection);
            cmd.ExecuteNonQuery();
        }
    }

    private static IEnumerable<string> SplitStoredProcedures(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            yield break;

        var regex = new Regex(
            @"SET\s+TERM\s+\S+\s*;\s*(?<body>.*?)SET\s+TERM\s*;\s*\^",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in regex.Matches(script))
        {
            var body = match.Groups["body"].Value;

            var procedures = body
                .Split(['^'], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p));

            foreach (var proc in procedures)
            {
                yield return proc;
            }
        }
    }

    private static void ExportDomains(FbConnection con, string outDir)
    {
        string sql = @"SELECT RDB$FIELD_NAME, RDB$VALIDATION_SOURCE FROM RDB$FIELDS WHERE RDB$FIELD_NAME NOT LIKE 'RDB$%'";
        using var cmd = new FbCommand(sql, con);
        using var reader = cmd.ExecuteReader();

        var sb = new StringBuilder();

        while (reader.Read())
        {
            string name = reader.GetString(0).Trim();
            sb.AppendLine($"CREATE DOMAIN {name} AS VARCHAR(100);");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(outDir, "1.domains.sql"), sb.ToString());
    }

    private static void ExportTables(FbConnection con, string outDir)
    {
        var sb = new StringBuilder();

        var sqlTables = @"SELECT RDB$RELATION_NAME 
                      FROM RDB$RELATIONS 
                      WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL
                      ORDER BY RDB$RELATION_NAME";

        using var cmdTables = new FbCommand(sqlTables, con);
        using var readerTables = cmdTables.ExecuteReader();

        while (readerTables.Read())
        {
            string tableName = readerTables.GetString(0).Trim();
            sb.AppendLine($"-- TABLE: {tableName}");
            sb.AppendLine($"CREATE TABLE {tableName} (");

            string sqlCols = @"
                SELECT rf.RDB$FIELD_NAME AS COL_NAME,
                        f.RDB$FIELD_NAME AS DOMAIN_NAME,
                        f.RDB$FIELD_TYPE,
                        f.RDB$FIELD_LENGTH,
                        rf.RDB$NULL_FLAG
                FROM RDB$RELATION_FIELDS rf
                JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                WHERE rf.RDB$RELATION_NAME = @tbl
                ORDER BY rf.RDB$FIELD_POSITION";

            using var cmdCols = new FbCommand(sqlCols, con);
            cmdCols.Parameters.AddWithValue("@tbl", tableName);
            using var readerCols = cmdCols.ExecuteReader();

            var colDefs = new List<string>();
            while (readerCols.Read())
            {
                string colName = readerCols.GetString(0).Trim();
                string type = readerCols.GetString(1).Trim(); // można mapować domenę lub typ
                int? nullFlag = readerCols.IsDBNull(4) ? null : readerCols.GetInt16(4);
                bool notNull = nullFlag == 1;

                colDefs.Add($"    {colName} {type}{(notNull ? " NOT NULL" : "")}");
            }

            sb.AppendLine(string.Join(",\n", colDefs));
            sb.AppendLine(");\n");

            string sqlPK = @"
                SELECT s.RDB$FIELD_NAME
                FROM RDB$RELATION_CONSTRAINTS rc
                JOIN RDB$INDEX_SEGMENTS s ON rc.RDB$INDEX_NAME = s.RDB$INDEX_NAME
                WHERE rc.RDB$RELATION_NAME = @tbl AND rc.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
                ORDER BY s.RDB$FIELD_POSITION";

            using var cmdPK = new FbCommand(sqlPK, con);
            cmdPK.Parameters.AddWithValue("@tbl", tableName);
            using var readerPK = cmdPK.ExecuteReader();
            var pkCols = new List<string>();
            while (readerPK.Read()) pkCols.Add(readerPK.GetString(0).Trim());

            if (pkCols.Count > 0)
            {
                sb.AppendLine($"ALTER TABLE {tableName} ADD PRIMARY KEY ({string.Join(", ", pkCols)});");
            }

            sb.AppendLine();
        }

        string sqlFK = @"
                SELECT rc.RDB$CONSTRAINT_NAME,
                       r1.RDB$RELATION_NAME AS TABLE_FROM,
                       s1.RDB$FIELD_NAME AS COL_FROM,
                       r2.RDB$RELATION_NAME AS TABLE_TO,
                       s2.RDB$FIELD_NAME AS COL_TO
                FROM RDB$REF_CONSTRAINTS rc
                JOIN RDB$RELATION_CONSTRAINTS r1 ON rc.RDB$CONSTRAINT_NAME = r1.RDB$CONSTRAINT_NAME
                JOIN RDB$RELATION_CONSTRAINTS r2 ON rc.RDB$CONST_NAME_UQ = r2.RDB$CONSTRAINT_NAME
                JOIN RDB$INDEX_SEGMENTS s1 ON r1.RDB$INDEX_NAME = s1.RDB$INDEX_NAME
                JOIN RDB$INDEX_SEGMENTS s2 ON r2.RDB$INDEX_NAME = s2.RDB$INDEX_NAME
                ";

        using var cmdFK = new FbCommand(sqlFK, con);
        using var readerFK = cmdFK.ExecuteReader();

        while (readerFK.Read())
        {
            string tableFrom = readerFK.GetString(1).Trim();
            string colFrom = readerFK.GetString(2).Trim();
            string tableTo = readerFK.GetString(3).Trim();
            string colTo = readerFK.GetString(4).Trim();
            sb.AppendLine($"ALTER TABLE {tableFrom} ADD CONSTRAINT FK_{tableFrom}_{colFrom} FOREIGN KEY ({colFrom}) REFERENCES {tableTo} ({colTo});");
        }

        File.WriteAllText(Path.Combine(outDir, "2.tables.sql"), sb.ToString());
    }

    private static void ExportProcedures(FbConnection con, string outDir)
    {
        Directory.CreateDirectory(outDir);

        var sb = new StringBuilder();

        // 1. SET TERM na początku
        sb.AppendLine("SET TERM ^ ;\n");

        // 2. CREATE PROCEDURE
        string sqlProcs = @"SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE
                        FROM RDB$PROCEDURES
                        WHERE RDB$SYSTEM_FLAG = 0
                        ORDER BY RDB$PROCEDURE_NAME";

        using var cmd = new FbCommand(sqlProcs, con);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            string procName = reader.GetString(0).Trim();
            string procSource = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

            // 3. Pobranie parametrów procedury
            string sqlParams = @"SELECT RDB$PARAMETER_NAME, RDB$FIELD_SOURCE, RDB$PARAMETER_TYPE
                             FROM RDB$PROCEDURE_PARAMETERS
                             WHERE RDB$PROCEDURE_NAME = @proc
                             ORDER BY RDB$PARAMETER_NUMBER";

            using var cmdParams = new FbCommand(sqlParams, con);
            cmdParams.Parameters.AddWithValue("@proc", procName);
            using var readerParams = cmdParams.ExecuteReader();

            var inParams = new List<string>();
            var outParams = new List<string>();

            while (readerParams.Read())
            {
                string paramName = readerParams.GetString(0).Trim();
                string typeName = readerParams.GetString(1).Trim();
                int paramType = readerParams.GetInt16(2); // 0 = input, 1 = output

                string decl = $"{paramName} {typeName}";

                if (paramType == 0)
                    inParams.Add(decl);
                else
                    outParams.Add(decl);
            }

            // 4. CREATE PROCEDURE z parametrami
            sb.AppendLine($"CREATE PROCEDURE {procName} ({string.Join(", ", inParams)})");
            sb.AppendLine("AS");
            sb.AppendLine("BEGIN");
            sb.AppendLine("  EXIT;"); // Uproszczona wersja CREATE
            sb.AppendLine("END^");
            sb.AppendLine();
        }

        // 5. ALTER PROCEDURE – w tym przykładzie możemy wykorzystać faktyczne źródła
        reader.Close();
        using var cmd2 = new FbCommand(sqlProcs, con);
        using var reader2 = cmd2.ExecuteReader();

        while (reader2.Read())
        {
            string procName = reader2.GetString(0).Trim();
            string procSource = reader2.IsDBNull(1) ? "" : reader2.GetString(1).Trim();

            if (string.IsNullOrWhiteSpace(procSource)) continue;

            // pobranie parametrów wejściowych dla ALTER
            string sqlParams = @"SELECT RDB$PARAMETER_NAME, RDB$FIELD_SOURCE, RDB$PARAMETER_TYPE
                             FROM RDB$PROCEDURE_PARAMETERS
                             WHERE RDB$PROCEDURE_NAME = @proc
                             AND RDB$PARAMETER_TYPE = 0
                             ORDER BY RDB$PARAMETER_NUMBER";

            using var cmdParams = new FbCommand(sqlParams, con);
            cmdParams.Parameters.AddWithValue("@proc", procName);
            using var readerParams = cmdParams.ExecuteReader();

            var inParams = new List<string>();
            while (readerParams.Read())
            {
                string paramName = readerParams.GetString(0).Trim();
                string typeName = readerParams.GetString(1).Trim();
                inParams.Add($"{paramName} {typeName}");
            }

            sb.AppendLine($"ALTER PROCEDURE {procName} ({string.Join(", ", inParams)})");
            sb.AppendLine("AS");
            sb.AppendLine(procSource);
            sb.AppendLine("END^\n");
        }

        // 6. SET TERM na końcu
        sb.AppendLine("SET TERM ; ^");

        File.WriteAllText(Path.Combine(outDir, "3.stored_procedures.sql"), sb.ToString());
    }
}
