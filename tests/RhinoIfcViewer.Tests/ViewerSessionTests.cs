using System;
using System.IO;
using Xunit;

namespace RhinoIfcViewer.Tests;

public sealed class ViewerSessionTests
{
    private static (string viewerDir, string sessionsDir, string tempRoot) SetupTestDirs()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerSessionTests", Guid.NewGuid().ToString("N"));
        var viewerDir = Path.Combine(tempRoot, "viewer");
        var sessionsDir = Path.Combine(tempRoot, "sessions");
        Directory.CreateDirectory(viewerDir);
        Directory.CreateDirectory(sessionsDir);
        File.WriteAllText(Path.Combine(viewerDir, "index.html"), "<html><body>Viewer</body></html>");
        return (viewerDir, sessionsDir, tempRoot);
    }

    [Fact]
    public void NewSession_and_two_revisions_produce_separate_contained_paths_and_uris()
    {
        var (viewerDir, sessionsDir, tempRoot) = SetupTestDirs();
        try
        {
            var session = new ViewerSession(viewerDir, sessionsDir);
            Assert.True(Directory.Exists(session.SiteDirectory));
            Assert.True(File.Exists(Path.Combine(session.SiteDirectory, "index.html")));

            string rev1 = "0123456789abcdef0123456789abcdef";
            string rev2 = "fedcba9876543210fedcba9876543210";

            string path1 = session.CreateModelPath(rev1);
            string path2 = session.CreateModelPath(rev2);

            Assert.NotEqual(path1, path2);
            Assert.StartsWith(session.SiteDirectory, path1);
            Assert.StartsWith(session.SiteDirectory, path2);
            Assert.EndsWith($"model-{rev1}.ifc", path1);
            Assert.EndsWith($"model-{rev2}.ifc", path2);

            var uri1 = session.CreateViewerUri(rev1, true);
            var uri2 = session.CreateViewerUri(rev2, false);

            Assert.Equal("https", uri1.Scheme);
            Assert.Equal("rhino-ifc.local", uri1.Host);
            Assert.Equal("/index.html", uri1.AbsolutePath);
            Assert.Contains($"revision={rev1}", uri1.Query);
            Assert.Contains("model=models%2Fmodel-" + rev1 + ".ifc", uri1.Query);

            Assert.Contains($"revision={rev2}", uri2.Query);
            Assert.DoesNotContain("model=", uri2.Query);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("0123456789ABCDEF0123456789ABCDEF")] // Uppercase
    [InlineData("../0123456789abcdef0123456789abcdef")] // Path traversal
    [InlineData("0123456789abcdef")] // Too short
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef")] // Too long
    [InlineData("0123456789abcdef0123456789abcdeg")] // Invalid hex 'g'
    [InlineData("")]
    public void Invalid_revisions_are_rejected(string invalidRevision)
    {
        var (viewerDir, sessionsDir, tempRoot) = SetupTestDirs();
        try
        {
            var session = new ViewerSession(viewerDir, sessionsDir);
            Assert.Throws<ArgumentException>(() => session.CreateModelPath(invalidRevision));
            Assert.Throws<ArgumentException>(() => session.CreateViewerUri(invalidRevision, true));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Source_and_staging_with_spaces_and_non_ascii_copy_cleanly()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerSessionTests", Guid.NewGuid().ToString("N"));
        var viewerDir = Path.Combine(tempRoot, "مجلد العارض مع مسافات");
        var sessionsDir = Path.Combine(tempRoot, "جلسات العمل");
        Directory.CreateDirectory(viewerDir);
        Directory.CreateDirectory(sessionsDir);
        File.WriteAllText(Path.Combine(viewerDir, "index.html"), "<html><body>UTF8</body></html>");
        var subDir = Path.Combine(viewerDir, "assets sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "bundle.js"), "console.log(1);");

        try
        {
            var session = new ViewerSession(viewerDir, sessionsDir);
            Assert.True(File.Exists(Path.Combine(session.SiteDirectory, "index.html")));
            Assert.True(File.Exists(Path.Combine(session.SiteDirectory, "assets sub", "bundle.js")));

            string rev = "0123456789abcdef0123456789abcdef";
            var uri = session.CreateViewerUri(rev, true);
            Assert.Equal("https://rhino-ifc.local/index.html?revision=0123456789abcdef0123456789abcdef&model=models%2Fmodel-0123456789abcdef0123456789abcdef.ifc", uri.AbsoluteUri);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
