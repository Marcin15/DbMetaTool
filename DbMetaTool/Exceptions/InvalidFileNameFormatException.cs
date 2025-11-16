namespace DbMetaTool.Exceptions;

internal sealed class InvalidFileNameFormatException(string fileName)
    : Exception($"Invalid file name format: {fileName}") { }
