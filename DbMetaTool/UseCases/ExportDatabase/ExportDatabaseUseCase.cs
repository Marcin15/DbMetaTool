using DbMetaTool.UseCases.ExportDatabase.ExportDatabaseStrategies;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.UseCases.ExportDatabase;

internal sealed class ExportDatabaseUseCase
{
    public static void Invoke(string connectionString, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        using var connection = new FbConnection(connectionString);
        connection.Open();

        RunStrategies(connection, outputDirectory);
    }

    private static void RunStrategies(FbConnection connection, string outputDirectory)
    {
        var exportDatabaseStrategyType = typeof(IExportDatabaseStrategy);
        var assembly = exportDatabaseStrategyType.Assembly;

        var strategyTypes = assembly.GetTypes()
            .Where(t => exportDatabaseStrategyType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var strategyType in strategyTypes)
        {
            var strategy = (IExportDatabaseStrategy)Activator.CreateInstance(strategyType)!;
            strategy.ExportDatabase(connection, outputDirectory);
        }
    }
}
