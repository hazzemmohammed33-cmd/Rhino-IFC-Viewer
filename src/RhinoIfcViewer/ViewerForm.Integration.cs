using System.Diagnostics;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Rhino;

namespace RhinoIfcViewer;

public sealed partial class ViewerForm
{
    private bool _firstOpenHandled;
    private ViewerSession? _session;
    private readonly CancellationTokenSource _windowLifetime = new();
    private Task? _initialization;
    private TaskCompletionSource<ViewerMessage>? _viewerCompletion;
    private string? _viewerRevision;
    private bool _browserHandlersAttached;
    private bool _browserNeedsReset;
    private bool _isBusy;
    private ViewerOperation? _operation;
    private bool _sceneAvailable;
    private ulong? _navigationId;
    private CancellationTokenSource? _currentOpCts;
    private string? _displayedRevision;
    private uint? _displayedDocumentSerial;
    private string? _latestExportPath;
    private int? _latestExportedCount;
    private int? _latestSkippedCount;
    private SnapshotMetadata? _displayedMetadata;
    private sealed record SnapshotMetadata(string DocumentName, string Path, int Exported, int Skipped);

    private bool CanPublish(ViewerOperation op) => !IsDisposed && !Disposing &&
        ReferenceEquals(_operation, op) && op.CanPublish(RhinoDoc.ActiveDoc?.RuntimeSerialNumber);

    private void InitializeIntegration()
    {
        RefreshRequested += OnRefreshRequested;
        RhinoDoc.ActiveDocumentChanged += OnActiveDocumentChanged;
        RhinoDoc.CloseDocument += OnCloseDocument;

        var doc = RhinoDoc.ActiveDoc;
        if (doc != null)
        {
            string name = string.IsNullOrEmpty(doc.Name) ? "Untitled" : doc.Name;
            SetSourceDocument(name);
            SetStatus("Ready. Click Export & Refresh to preview IFC.", UiStatusKind.Neutral);
        }
        else
        {
            SetSourceDocument("No document");
            SetStatus("No active document.", UiStatusKind.Neutral);
        }
        SetPreviewMessage("No preview yet", "Click Export & Refresh or continue editing in Rhino.");
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_firstOpenHandled)
            return;
        _firstOpenHandled = true;
        // Trigger initial automatic refresh once after first form shown.
        await RefreshAsync();
    }

    private void OnActiveDocumentChanged(object? sender, DocumentEventArgs e)
    {
        if (IsDisposed || Disposing) return;
        _operation?.Invalidate();
        if (InvokeRequired)
        {
            BeginInvoke(new Action(HandleDocumentChanged));
            return;
        }
        HandleDocumentChanged();
    }

    private void OnCloseDocument(object? sender, DocumentEventArgs e)
    {
        if (IsDisposed || Disposing) return;
        uint? closing = e.Document?.RuntimeSerialNumber;
        if (closing != _operation?.DocumentSerial && closing != _displayedDocumentSerial &&
            closing != RhinoDoc.ActiveDoc?.RuntimeSerialNumber) return;
        _operation?.Invalidate();
        if (InvokeRequired)
        {
            BeginInvoke(new Action(HandleDocumentChanged));
            return;
        }
        HandleDocumentChanged();
    }

    private void HandleDocumentChanged()
    {
        if (IsDisposed || Disposing) return;

        // Invalidate in-flight operation
        _operation?.Invalidate();
        ClearScene();

        // Invalidate displayed scene eligibility
        _displayedRevision = null;
        _displayedDocumentSerial = null;
        _latestExportPath = null;
        _latestExportedCount = null;
        _latestSkippedCount = null;

        ClearExportSummary();
        SetExportPath(null);
        SetSnapshotStale(false);

        var activeDoc = RhinoDoc.ActiveDoc;
        if (activeDoc != null)
        {
            string name = string.IsNullOrEmpty(activeDoc.Name) ? "Untitled" : activeDoc.Name;
            SetSourceDocument(name);
            SetStatus("Document changed. Click Export & Refresh to update.", UiStatusKind.Neutral);
            SetPreviewMessage("Document changed", "Click Export & Refresh to preview the active document.");
        }
        else
        {
            SetSourceDocument("No document");
            SetStatus("No active document.", UiStatusKind.Neutral);
            SetPreviewMessage("No active document", "Open a Rhino document to preview IFC.");
        }
    }

    private async void OnRefreshRequested(object? sender, EventArgs e) => await RefreshAsync();

    public async Task RefreshAsync()
    {
        if (IsDisposed || Disposing) return;
        if (_isBusy)
        {
            RhinoApp.WriteLine("IFC viewer is busy. Please wait for current operation to complete.");
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            ClearScene();
            _latestExportPath = null;
            _latestExportedCount = null;
            _latestSkippedCount = null;
            _displayedRevision = null;
            _displayedDocumentSerial = null;
            ClearExportSummary();
            SetExportPath(null);
            SetSnapshotStale(false);
            SetSourceDocument("No document");
            SetStatus("No active document.", UiStatusKind.Neutral);
            SetPreviewMessage("No active document", "Open a Rhino document to export and preview.");
            return;
        }

        _isBusy = true;
        SetBusy(true);

        _currentOpCts?.Dispose();
        var operation = new ViewerOperation(doc.RuntimeSerialNumber,
            string.IsNullOrWhiteSpace(doc.Name) ? "Untitled" : doc.Name, _windowLifetime.Token);
        _operation = operation;
        _currentOpCts = operation.Cancellation;
        var opToken = _currentOpCts.Token;

        uint capturedSerial = doc.RuntimeSerialNumber;
        string capturedName = string.IsNullOrEmpty(doc.Name) ? "Untitled" : doc.Name;
        string revision = operation.Revision;

        try
        {
            SetStatus("Initializing viewer...", UiStatusKind.Busy);
            await EnsureViewerAsync();
            opToken.ThrowIfCancellationRequested();

            SetStatus("Reading Rhino model geometry...", UiStatusKind.Busy);
            var sw = Stopwatch.StartNew();
            ModelSnapshot snapshot;
            try
            {
                snapshot = new RhinoMeshReader().Read(doc);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"RhinoMeshReader failed: {ex}");
                if (CanPublish(operation)) HandleCaptureOrExportFailure(ex.Message, capturedSerial);
                return;
            }
            sw.Stop();
            RhinoApp.WriteLine($"Captured snapshot in {sw.ElapsedMilliseconds} ms: {snapshot.Objects.Count} objects, {snapshot.Exclusions.Count} exclusions");

            foreach (var excl in snapshot.Exclusions)
            {
                RhinoApp.WriteLine($"[IFC Exclusion] {excl}");
            }

            opToken.ThrowIfCancellationRequested();

            if (snapshot.Objects.Count == 0)
            {
                ClearScene();
                _latestExportPath = null;
                _latestExportedCount = null;
                _latestSkippedCount = null;
                SetExportPath(null);
                SetExportSummary(0, snapshot.Exclusions.Count);
                SetSnapshotStale(false);
                SetSourceDocument(capturedName);
                string emptyDetail = snapshot.Exclusions.Count > 0
                    ? $"No supported geometry found. {snapshot.Exclusions.Count} object(s) excluded."
                    : "No supported 3D geometry found in document.";
                SetPreviewMessage("Empty document", emptyDetail);
                SetStatus(snapshot.Exclusions.Count > 0 ? "Loaded with exclusions (0 objects)" : "No renderable geometry found.",
                    snapshot.Exclusions.Count > 0 ? UiStatusKind.Warning : UiStatusKind.Neutral);
                return;
            }

            SetStatus("Exporting IFC4 model...", UiStatusKind.Busy);
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OptiDesign", "RhinoIfcViewer", "exports");
            // IfcExporter creates the directory on the worker thread, inside
            // the export-failure boundary that preserves an eligible old scene.
            string finalPath = Path.Combine(exportDir, $"model-{revision}.ifc");

            ExportResult exportResult;
            try
            {
                var exporter = new IfcExporter();
                exportResult = await Task.Run(() => exporter.Write(snapshot, finalPath, opToken), opToken);
            }
            catch (OperationCanceledException) when (opToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"IfcExporter failed: {ex}");
                if (CanPublish(operation)) HandleCaptureOrExportFailure(ex.Message, capturedSerial);
                return;
            }

            if (IsDisposed || Disposing || opToken.IsCancellationRequested) return;
            if (RhinoDoc.ActiveDoc == null || RhinoDoc.ActiveDoc.RuntimeSerialNumber != capturedSerial)
            {
                RhinoApp.WriteLine("Active document changed during export. Discarding output for current view.");
                return;
            }

            _latestExportPath = exportResult.FilePath;
            _latestExportedCount = exportResult.ExportedCount;
            _latestSkippedCount = snapshot.Exclusions.Count;

            SetExportPath(exportResult.FilePath);
            SetExportSummary(exportResult.ExportedCount, snapshot.Exclusions.Count);
            SetStatus("Loading IFC preview...", UiStatusKind.Busy);

            string sessionModelPath = _session!.CreateModelPath(revision);
            try
            {
                await Task.Run(() => File.Copy(exportResult.FilePath, sessionModelPath, true), opToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                RhinoApp.WriteLine($"Viewer file staging failed: {ex}");
                if (CanPublish(operation))
                    HandleCaptureOrExportFailure("Viewer file staging failed: " + ex.Message, capturedSerial);
                return;
            }
            opToken.ThrowIfCancellationRequested();

            _viewerRevision = revision;
            _viewerCompletion = operation.Completion;
            _displayedRevision = null;
            _displayedDocumentSerial = null;
            _sceneAvailable = false;
            _displayedMetadata = null;
            SetSnapshotStale(false);

            SetPreviewMessage(null, null);
            ViewerBrowser.CoreWebView2.Navigate(_session.CreateViewerUri(revision, true).AbsoluteUri);

            ViewerMessage message = await _viewerCompletion.Task.WaitAsync(TimeSpan.FromSeconds(60), opToken);

            if (IsDisposed || Disposing || opToken.IsCancellationRequested) return;
            if (RhinoDoc.ActiveDoc == null || RhinoDoc.ActiveDoc.RuntimeSerialNumber != capturedSerial) return;

            if (message.Type == "loaded" && operation.ViewerFailure is null)
            {
                _sceneAvailable = true;
                _displayedRevision = revision;
                _displayedDocumentSerial = capturedSerial;
                _displayedMetadata = new(capturedName, exportResult.FilePath, exportResult.ExportedCount, snapshot.Exclusions.Count);
                SetSourceDocument(capturedName);
                SetSnapshotStale(false);
                string status = snapshot.Exclusions.Count > 0 ? "Loaded with exclusions" : "Loaded";
                SetStatus(status, snapshot.Exclusions.Count > 0 ? UiStatusKind.Warning : UiStatusKind.Success);
                RhinoApp.WriteLine($"IFC preview loaded: revision={revision}; objects={exportResult.ExportedCount}; exclusions={snapshot.Exclusions.Count}");
            }
            else
            {
                _viewerRevision = null;
                string errorMsg = operation.ViewerFailure ?? message.Message ?? "Unknown viewer error";
                ClearScene();
                SetStatus($"Viewer error: {errorMsg}", UiStatusKind.Error);
                SetPreviewMessage("Viewer error", $"{errorMsg}. IFC exported, not displayed. Click Export & Refresh to retry.");
            }
        }
        catch (OperationCanceledException) when (opToken.IsCancellationRequested || _windowLifetime.IsCancellationRequested)
        {
            // Cancelled
        }
        catch (TimeoutException)
        {
            _viewerRevision = null;
            RhinoApp.WriteLine("IFC viewer timed out after 60 seconds.");
            if (CanPublish(operation))
            {
                ClearScene();
                SetStatus("Viewer timed out after 60 seconds.", UiStatusKind.Error);
                SetPreviewMessage("Viewer timeout", "Viewer took longer than 60 seconds to render. Click Export & Refresh to retry.");
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Refresh error: {ex}");
            if (CanPublish(operation))
            {
                ClearScene();
                SetStatus($"Error: {ex.Message}", UiStatusKind.Error);
                SetPreviewMessage("Error", ex.Message +
                    " Any retained IFC file is exported, not displayed. Click Export & Refresh to retry.");
            }
        }
        finally
        {
            if (ReferenceEquals(_operation, operation))
            {
                _operation = null;
                _viewerCompletion = null;
                _viewerRevision = null;
                _navigationId = null;
                _currentOpCts = null;
                _isBusy = false;
                if (!IsDisposed && !Disposing) SetBusy(false);
            }
            operation.Dispose();
        }
    }

    private void HandleCaptureOrExportFailure(string error, uint capturedSerial)
    {
        if (IsDisposed || Disposing) return;
        if (_sceneAvailable && _displayedRevision != null && _displayedDocumentSerial == capturedSerial)
        {
            if (_displayedMetadata is { } metadata)
            {
                SetSourceDocument(metadata.DocumentName);
                SetExportPath(metadata.Path);
                SetExportSummary(metadata.Exported, metadata.Skipped);
            }
            SetPreviewMessage(null, null);
            SetSnapshotStale(true);
            SetStatus($"Export failed: {error}", UiStatusKind.Error);
        }
        else
        {
            _displayedRevision = null;
            _displayedDocumentSerial = null;
            SetSnapshotStale(false);
            SetStatus($"Export failed: {error}", UiStatusKind.Error);
            SetPreviewMessage("Export failed", $"{error}. Any retained IFC file is exported, not displayed. Click Export & Refresh to retry.");
        }
    }

    private async Task EnsureViewerAsync()
    {
        if (_browserNeedsReset) ResetBrowser();
        try { await (_initialization ??= InitializeViewerAsync()); }
        catch { _initialization = null; _browserNeedsReset = true; throw; }
    }

    private void DetachBrowserHandlers()
    {
        if (!_browserHandlersAttached || ViewerBrowser.CoreWebView2 is not { } core) return;
        core.NavigationStarting -= OnNavigationStarting;
        core.NavigationCompleted -= OnNavigationCompleted;
        core.WebMessageReceived -= OnWebMessageReceived;
        core.ProcessFailed -= OnProcessFailed;
        core.NewWindowRequested -= OnNewWindowRequested;
        _browserHandlersAttached = false;
    }

    private void ResetBrowser()
    {
        DetachBrowserHandlers();
        var previous = ViewerBrowser;
        var parent = previous.Parent!;
        int index = parent.Controls.GetChildIndex(previous);
        parent.Controls.Remove(previous);
        previous.Dispose();
        ViewerBrowser = new Microsoft.Web.WebView2.WinForms.WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false,
            AccessibleName = "IFC model preview",
            TabIndex = 1
        };
        parent.Controls.Add(ViewerBrowser);
        parent.Controls.SetChildIndex(ViewerBrowser, index);
        _initialization = null;
        _session = null;
        _browserNeedsReset = false;
        _sceneAvailable = false;
        _displayedRevision = null;
        _displayedDocumentSerial = null;
        _displayedMetadata = null;
    }

    private void ClearScene()
    {
        _sceneAvailable = false;
        _displayedRevision = null;
        _displayedDocumentSerial = null;
        _displayedMetadata = null;
        _viewerRevision = null;
        _viewerCompletion = null;
        _navigationId = null;
        SetSnapshotStale(false);
        if (_session is null || ViewerBrowser.CoreWebView2 is not { } core) return;
        try
        {
            core.Stop();
            core.Navigate(_session.CreateViewerUri(Guid.NewGuid().ToString("N"), false).AbsoluteUri);
        }
        catch (Exception error)
        {
            _browserNeedsReset = true;
            RhinoApp.WriteLine("Viewer page clear failed: " + error.Message);
        }
    }

    private async Task InitializeViewerAsync()
    {
        string storage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OptiDesign", "RhinoIfcViewer");
        string viewer = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "viewer");
        var session = await Task.Run(() => new ViewerSession(viewer, Path.Combine(storage, "sessions")), _windowLifetime.Token);
        _windowLifetime.Token.ThrowIfCancellationRequested();

        var alc = AssemblyLoadContext.GetLoadContext(GetType().Assembly);
        using var reflectionScope = alc?.EnterContextualReflection();

        var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(storage, "WebView2"));
        _windowLifetime.Token.ThrowIfCancellationRequested();
        await ViewerBrowser.EnsureCoreWebView2Async(environment);
        _windowLifetime.Token.ThrowIfCancellationRequested();
        var core = ViewerBrowser.CoreWebView2;
        core.SetVirtualHostNameToFolderMapping("rhino-ifc.local", session.SiteDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.WebMessageReceived += OnWebMessageReceived;
        core.ProcessFailed += OnProcessFailed;
        core.NewWindowRequested += OnNewWindowRequested;
        _browserHandlersAttached = true;
        _session = session;
    }

    private static bool IsLocalUri(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == "https" && uri.Host == "rhino-ifc.local" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo);

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!IsLocalUri(e.Uri)) { e.Cancel = true; return; }
        if (_operation is { } op && CanPublish(op) && _viewerRevision == op.Revision &&
            e.Uri == _session?.CreateViewerUri(op.Revision, true).AbsoluteUri)
            _navigationId = e.NavigationId;
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e) => e.Handled = true;

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess && e.NavigationId == _navigationId && _operation is { } op && CanPublish(op))
            op.TryComplete("error", op.Revision, $"Viewer navigation failed: {e.WebErrorStatus}",
                RhinoDoc.ActiveDoc?.RuntimeSerialNumber);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (IsDisposed || Disposing || !IsLocalUri(e.Source)) return;
        try
        {
            var message = JsonSerializer.Deserialize<ViewerMessage>(e.WebMessageAsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (message is null) return;
            if (_operation is { } op && CanPublish(op) && _viewerRevision == op.Revision)
                op.TryComplete(message.Type, message.Revision, message.Message, RhinoDoc.ActiveDoc?.RuntimeSerialNumber);
            if (_sceneAvailable && _displayedRevision != null && message.Type == "error" && !string.IsNullOrWhiteSpace(message.Message) &&
                message.Revision == _displayedRevision && _displayedDocumentSerial == RhinoDoc.ActiveDoc?.RuntimeSerialNumber)
            {
                ClearScene();
                SetStatus("Viewer failed: " + message.Message, UiStatusKind.Error);
                SetPreviewMessage("Viewer failed", message.Message + " IFC file remains exported, not displayed.");
            }
        }
        catch (JsonException error) { RhinoApp.WriteLine("Ignored invalid viewer message: " + error.Message); }
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (IsDisposed || Disposing || !ReferenceEquals(sender, ViewerBrowser.CoreWebView2)) return;
        string message = $"WebView2 process failed: {e.ProcessFailedKind}";
        bool affectsCurrentSource = (_operation is { } op && CanPublish(op)) ||
            (_sceneAvailable && _displayedDocumentSerial == RhinoDoc.ActiveDoc?.RuntimeSerialNumber);
        _browserNeedsReset = true;
        _initialization = null;
        if (_operation is { } current && CanPublish(current))
            current.TryComplete("error", current.Revision, message, RhinoDoc.ActiveDoc?.RuntimeSerialNumber);
        _displayedRevision = null;
        _displayedDocumentSerial = null;
        _displayedMetadata = null;
        _sceneAvailable = false;
        if (affectsCurrentSource)
        {
            SetSnapshotStale(false);
            SetStatus(message, UiStatusKind.Error);
            SetPreviewMessage("Viewer failed", message +
                ". Any retained IFC file is exported, not displayed. Click Export & Refresh to retry.");
        }
    }

    private void DisposeIntegration()
    {
        RefreshRequested -= OnRefreshRequested;
        RhinoDoc.ActiveDocumentChanged -= OnActiveDocumentChanged;
        RhinoDoc.CloseDocument -= OnCloseDocument;
        _windowLifetime.Cancel();
        _operation?.Invalidate();
        _viewerCompletion?.TrySetCanceled();
        DetachBrowserHandlers();
    }
}
