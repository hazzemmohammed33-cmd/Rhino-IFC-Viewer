namespace RhinoIfcViewer;

public sealed class ViewerSession
{
    public string SiteDirectory { get; }

    public ViewerSession(string viewerDirectory, string sessionsDirectory)
    {
        string source = Path.GetFullPath(viewerDirectory);
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Bundled viewer directory is missing: {source}");
        if (!File.Exists(Path.Combine(source, "index.html")))
            throw new FileNotFoundException("Bundled viewer index.html is missing.", Path.Combine(source, "index.html"));
        string session = Path.Combine(Path.GetFullPath(sessionsDirectory), Guid.NewGuid().ToString("N"));
        SiteDirectory = Path.Combine(session, "site");
        CopyDirectory(source, SiteDirectory);
        Directory.CreateDirectory(Path.Combine(SiteDirectory, "models"));
    }

    public string CreateModelPath(string revision)
    {
        ValidateRevision(revision);
        return Path.Combine(SiteDirectory, "models", $"model-{revision}.ifc");
    }

    public Uri CreateViewerUri(string revision, bool hasModel)
    {
        ValidateRevision(revision);
        string query = "revision=" + Uri.EscapeDataString(revision);
        if (hasModel)
            query += "&model=" + Uri.EscapeDataString($"models/model-{revision}.ifc");
        return new UriBuilder("https", "rhino-ifc.local") { Path = "/index.html", Query = query }.Uri;
    }

    private static void ValidateRevision(string revision)
    {
        if (revision is null || revision.Length != 32 || revision.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("Revision must be 32 lowercase hexadecimal characters.", nameof(revision));
    }

    private static void CopyDirectory(string source, string destination)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Viewer staging does not follow directory links.");
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Viewer staging does not follow file links.");
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        foreach (string directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
}
