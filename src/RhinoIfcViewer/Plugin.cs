using System.Runtime.InteropServices;
using Rhino;

namespace RhinoIfcViewer;

[Guid("5f99a061-1094-4edf-a28a-c88b4c0d3391")]
public sealed class Plugin : Rhino.PlugIns.PlugIn
{
    private Form? _viewer;
    private readonly ViewerLoadContext _viewerContext = new();

    public Plugin() => Instance = this;

    public static Plugin Instance { get; private set; } = null!;

    internal void OpenViewer()
    {
        // Rhino invokes commands on its UI thread. Reuse its existing message loop.
        if (_viewer is { IsDisposed: false })
        {
            if (_viewer.WindowState == FormWindowState.Minimized)
                _viewer.WindowState = FormWindowState.Normal;
            _viewer.Activate();
            return;
        }

        var viewer = _viewerContext.CreateForm();
        _viewer = viewer;
        viewer.FormClosed += OnViewerClosed;
        try
        {
            viewer.Show(new RhinoWindow(RhinoApp.MainWindowHandle()));
        }
        catch
        {
            viewer.FormClosed -= OnViewerClosed;
            _viewer = null;
            viewer.Dispose();
            throw;
        }
    }

    private void OnViewerClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is Form viewer)
            viewer.FormClosed -= OnViewerClosed;
        if (ReferenceEquals(_viewer, sender))
            _viewer = null;
    }

    protected override void OnShutdown()
    {
        var viewer = _viewer;
        _viewer = null;
        if (viewer is not null)
        {
            viewer.FormClosed -= OnViewerClosed;
            viewer.Close();
            viewer.Dispose();
        }
        base.OnShutdown();
    }

    private sealed class RhinoWindow(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
