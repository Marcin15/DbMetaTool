using FirebirdSql.Data.FirebirdClient;
using System.Text.RegularExpressions;

namespace DbMetaTool.Extensions;

internal static class FbConnectionExtensions
{
    public static void ExecuteScript(this FbConnection connection, string script)
    {
        if (script.Contains("SET TERM"))
        {
            var storedProceduresCommands = SplitStoredProcedures(script);
            connection.ExecuteCommands(storedProceduresCommands);

            return;
        }

        var commands = script.Split(";");
        connection.ExecuteCommands(commands);
    }

    private static void ExecuteCommands(this FbConnection connection, IEnumerable<string> commands)
    {
        foreach (var command in commands)
        {
            var trimmed = command.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

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
                .Select(p => p.Trim().Replace("^", string.Empty))
                .Where(p => !string.IsNullOrWhiteSpace(p));

            foreach (var proc in procedures)
            {
                yield return proc;
            }
        }
    }
}
