using System.Runtime.InteropServices;
using Rhino;
using Rhino.Commands;

namespace RhinoIfcViewer;

[Guid("6ab87051-39a2-4ff4-87e4-a60c859bc1dd")]
public sealed class OpenViewerCommand : Command
{
    public override string EnglishName => "OptiIfcViewer";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        try
        {
            Plugin.Instance.OpenViewer();
            return Result.Success;
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Unable to open IFC Preview: {ex.Message}");
            return Result.Failure;
        }
    }
}
