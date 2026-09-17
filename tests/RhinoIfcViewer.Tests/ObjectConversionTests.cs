using Xunit;

namespace RhinoIfcViewer.Tests;

public sealed class ObjectConversionTests
{
    [Fact]
    public void Native_conversion_error_identifies_object_and_preserves_cause()
    {
        var id = Guid.NewGuid();
        var cause = new InvalidOperationException("Native mesher rejected geometry");
        var error = Assert.Throws<InvalidOperationException>(() =>
            ObjectConversion.Run(id, "Bad Brep", () => throw cause));
        Assert.Contains(id.ToString(), error.Message);
        Assert.Contains("Bad Brep", error.Message);
        Assert.Contains(cause.Message, error.Message);
        Assert.Same(cause, error.InnerException);
    }

    [Fact]
    public void Successful_conversion_runs_once()
    {
        int count = 0;
        ObjectConversion.Run(Guid.NewGuid(), "Box", () => count++);
        Assert.Equal(1, count);
    }
}
