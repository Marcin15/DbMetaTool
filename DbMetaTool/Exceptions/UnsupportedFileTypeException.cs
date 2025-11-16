namespace DbMetaTool.Exceptions;

internal sealed class UnsupportedFileTypeException(string filePath)
    : Exception($"Unsupported file type: {filePath}") { }
