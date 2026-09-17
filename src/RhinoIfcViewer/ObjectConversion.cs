namespace RhinoIfcViewer;

internal static class ObjectConversion
{
    internal static void Run(Guid id, string name, Action convert)
    {
        try { convert(); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        {
            throw new InvalidOperationException(
                $"Supported object '{name}' ({id}) conversion failed: {error.Message}", error);
        }
    }
}
