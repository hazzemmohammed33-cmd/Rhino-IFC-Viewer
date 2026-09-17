namespace RhinoIfcViewer;

internal sealed class ViewerOperation : IDisposable
{
    public string Revision { get; } = Guid.NewGuid().ToString("N");
    public uint DocumentSerial { get; }
    public string DocumentName { get; }
    public CancellationTokenSource Cancellation { get; }
    public TaskCompletionSource<ViewerMessage> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Invalidated { get; private set; }
    public string? ViewerFailure { get; private set; }
    public ViewerOperation(uint serial, string name, CancellationToken lifetime)
    {
        DocumentSerial = serial;
        DocumentName = name;
        Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
    }
    public bool CanPublish(uint? activeSerial) => !Invalidated && !Cancellation.IsCancellationRequested && activeSerial == DocumentSerial;
    public void Invalidate()
    {
        Invalidated = true;
        Cancellation.Cancel();
        Completion.TrySetCanceled();
    }
    public bool TryComplete(string? type, string? revision, string? message, uint? activeSerial)
    {
        if (!CanPublish(activeSerial) || revision != Revision || type is not ("loaded" or "error")) return false;
        if (type == "error" && string.IsNullOrWhiteSpace(message)) return false;
        // The acknowledgement's continuation may still be queued when a runtime
        // error arrives. Retain it even if loaded already completed the task.
        if (type == "error") ViewerFailure ??= message;
        return Completion.TrySetResult(new ViewerMessage(type, revision, message));
    }
    public void Dispose() => Cancellation.Dispose();
}

internal sealed record ViewerMessage(string? Type, string? Revision, string? Message);
