using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.UseCases.ExportDatabase.ExportDatabaseStrategies;

internal interface IExportDatabaseStrategy
{
    void ExportDatabase(FbConnection connection, string outputDirectory);
}
