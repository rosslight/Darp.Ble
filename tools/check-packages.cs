// Checks packed packages before they are published: each folder holds one package per library in src,
// all at the release version, and every library carries its PDB with Source Link and the repository it points to.
// Usage: dotnet run tools/check-packages.cs -- <version> <package folder>...
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

string version = args[0];
string[] expectedIds = Directory
    .EnumerateFiles("src", "*.csproj", SearchOption.AllDirectories)
    .Select(path => Path.GetFileNameWithoutExtension(path))
    .Order(StringComparer.Ordinal)
    .ToArray();
var errors = new List<string>();

foreach (string folder in args[1..])
{
    var ids = new List<string>();
    foreach (string path in Directory.EnumerateFiles(folder, "*.nupkg"))
    {
        using ZipArchive package = ZipFile.OpenRead(path);
        ZipArchiveEntry nuspecEntry = package.Entries.Single(entry =>
            entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal)
        );
        using Stream nuspecStream = nuspecEntry.Open();
        XElement[] metadata = XDocument.Load(nuspecStream).Descendants().ToArray();
        string id = metadata
            .First(element => string.Equals(element.Name.LocalName, "id", StringComparison.Ordinal))
            .Value;
        string packageVersion = metadata
            .First(element => string.Equals(element.Name.LocalName, "version", StringComparison.Ordinal))
            .Value;
        Console.WriteLine($"{path}: {id} {packageVersion}");

        ids.Add(id);
        if (!string.Equals(packageVersion, version, StringComparison.Ordinal))
            errors.Add($"{path} has version {packageVersion} instead of {version}");
        XElement? repository = metadata.FirstOrDefault(element =>
            string.Equals(element.Name.LocalName, "repository", StringComparison.Ordinal)
        );
        if (repository?.Attribute("url") is null || repository.Attribute("commit") is null)
            errors.Add($"{path} names no repository url and commit");
        foreach (
            ZipArchiveEntry library in package.Entries.Where(entry =>
                entry.FullName.StartsWith("lib/", StringComparison.Ordinal)
                && entry.FullName.EndsWith(".dll", StringComparison.Ordinal)
            )
        )
        {
            if (!HasEmbeddedSourceLink(library))
                errors.Add($"{path} has no embedded PDB with Source Link in {library.FullName}");
        }
    }
    string[] actualIds = ids.Order(StringComparer.Ordinal).ToArray();
    if (!actualIds.SequenceEqual(expectedIds, StringComparer.Ordinal))
        errors.Add(
            $"{folder} has the packages [{string.Join(", ", actualIds)}] instead of [{string.Join(", ", expectedIds)}]"
        );
}

foreach (string error in errors)
    Console.Error.WriteLine(error);
return errors.Count == 0 ? 0 : 1;

static bool HasEmbeddedSourceLink(ZipArchiveEntry library)
{
    // PEReader needs a seekable stream
    using var dll = new MemoryStream();
    using (Stream entryStream = library.Open())
        entryStream.CopyTo(dll);
    dll.Position = 0;
    using var peReader = new PEReader(dll);
    DebugDirectoryEntry[] embeddedPdbs = peReader
        .ReadDebugDirectory()
        .Where(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
        .ToArray();
    if (embeddedPdbs.Length == 0)
        return false;
    using MetadataReaderProvider pdbProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbs[0]);
    MetadataReader pdb = pdbProvider.GetMetadataReader();
    var sourceLinkKind = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");
    return pdb.CustomDebugInformation.Any(handle =>
        pdb.GetGuid(pdb.GetCustomDebugInformation(handle).Kind) == sourceLinkKind
    );
}
