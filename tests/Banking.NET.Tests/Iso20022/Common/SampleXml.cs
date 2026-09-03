using System.Reflection;

namespace Banking.NET.Tests.Iso20022.Common;

internal static class SampleXml
{
    private static readonly Assembly Assembly = typeof(SampleXml).Assembly;

    public static string Load(string fileName)
    {
        var resourceName = Assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(fileName, StringComparison.Ordinal))
            ?? throw new FileNotFoundException($"No embedded resource ending in '{fileName}' was found.");

        using var stream = Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
