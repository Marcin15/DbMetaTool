using DbMetaTool.Exceptions;
using DbMetaTool.Extensions;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.UseCases.UpdateDatabase;

internal sealed class UpdateDatabaseUseCase
{
    public static void Invoke(string connectionString, string scriptsDirectory)
    {
        using var connection = new FbConnection(connectionString);
        connection.Open();

        foreach (var file in Directory.GetFiles(scriptsDirectory).OrderBy(f => f))
        {
            if (!file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnsupportedFileTypeException(scriptsDirectory);
            }

            var fileName = Path.GetFileName(file);
            if (!char.IsNumber(fileName[0]))
            {
                throw new InvalidFileNameFormatException(scriptsDirectory);
            }

            var script = File.ReadAllText(file);
            connection.ExecuteScript(script);
        }
    }
}
