using DbMetaTool.Exceptions;
using DbMetaTool.Extensions;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.UseCases.BuildDatabase;

internal sealed class BuildDatabaseUseCase
{
    private const string _userId = "SYSDBA";
    private const string _password = "masterkey";
    private const string _dataSource = "localhost";

    public static void Invoke(string databaseDirectory, string scriptsDirectory)
    {
        var databaseFileDirectory = CreateDirectory(databaseDirectory);
        var connectionString = CreateDatabase(databaseFileDirectory);

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

    private static string CreateDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        return Path.Combine(directoryPath, "database.fdb"); ;
    }

    private static string CreateDatabase(string databasePath)
    {
        var connectionString = new FbConnectionStringBuilder
        {
            Database = databasePath,
            UserID = _userId,
            Password = _password,
            DataSource = _dataSource
        }.ToString();

        FbConnection.CreateDatabase(connectionString, overwrite: true);

        return connectionString;
    }
}
