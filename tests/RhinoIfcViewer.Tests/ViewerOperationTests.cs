using Xunit;

namespace RhinoIfcViewer.Tests;

public sealed class ViewerOperationTests
{
    [Fact]
    public void Failure_after_loaded_ack_is_retained_before_UI_publishes_success()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        Assert.True(operation.TryComplete("loaded", operation.Revision, null, 7));
        operation.TryComplete("error", operation.Revision, "Renderer lost", 7);
        Assert.Equal("Renderer lost", operation.ViewerFailure);
    }

    [Fact]
    public void Invalidated_or_wrong_revision_error_cannot_set_failure()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        operation.TryComplete("error", new string('f', 32), "Old error", 7);
        Assert.Null(operation.ViewerFailure);
        operation.Invalidate();
        operation.TryComplete("error", operation.Revision, "Late error", 7);
        Assert.Null(operation.ViewerFailure);
    }

    [Fact]
    public void Switching_away_and_back_never_revalidates_an_operation()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        Assert.True(operation.CanPublish(7));
        operation.Invalidate();
        Assert.False(operation.CanPublish(8));
        Assert.False(operation.CanPublish(7));
    }

    [Fact]
    public void Empty_and_old_messages_cannot_complete_a_model_load()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        Assert.False(operation.TryComplete("empty", operation.Revision, null, 7));
        Assert.False(operation.TryComplete("loaded", new string('a', 32), null, 7));
        Assert.False(operation.Completion.Task.IsCompleted);
        Assert.True(operation.TryComplete("loaded", operation.Revision, null, 7));
        Assert.False(operation.TryComplete("error", operation.Revision, "late", 7));
    }

    [Fact]
    public void Invalidated_failure_cannot_complete_or_publish()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        operation.Invalidate();
        Assert.False(operation.TryComplete("error", operation.Revision, "write failed", 7));
        Assert.False(operation.CanPublish(7));
    }

    [Fact]
    public void Unknown_and_malformed_messages_are_rejected()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        Assert.False(operation.TryComplete("unknown", operation.Revision, null, 7));
        Assert.False(operation.TryComplete("ping", operation.Revision, null, 7));
        Assert.False(operation.TryComplete(null, operation.Revision, null, 7));
        Assert.False(operation.TryComplete("error", operation.Revision, null, 7));
        Assert.False(operation.TryComplete("error", operation.Revision, "   ", 7));
        Assert.False(operation.Completion.Task.IsCompleted);
    }

    [Fact]
    public void Document_serial_mismatch_rejects_completion()
    {
        using var operation = new ViewerOperation(7, "Source", CancellationToken.None);
        Assert.False(operation.TryComplete("loaded", operation.Revision, null, 8));
        Assert.False(operation.TryComplete("loaded", operation.Revision, null, null));
        Assert.False(operation.Completion.Task.IsCompleted);
    }
}
