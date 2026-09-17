using System.Reflection;
using System.Runtime.Loader;

namespace RhinoIfcViewer;

// Rhino loads its own older WebView2 Core for its UI. Keep our pinned Core and
// WinForms pair together without replacing Rhino's assemblies or creating another project.
internal sealed class ViewerLoadContext : AssemblyLoadContext
{
    private readonly string _directory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!;

    public ViewerLoadContext() : base("RhinoIfcViewer.WebView2", isCollectible: false) { }

    public Form CreateForm()
    {
        Assembly assembly = LoadFromAssemblyPath(typeof(Plugin).Assembly.Location);
        return (Form)Activator.CreateInstance(assembly.GetType("RhinoIfcViewer.ViewerForm", throwOnError: true)!)!;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is "Microsoft.Web.WebView2.Core" or "Microsoft.Web.WebView2.WinForms")
            return LoadFromAssemblyPath(Path.Combine(_directory, assemblyName.Name + ".dll"));
        // Share RhinoCommon, WinForms and other framework types with the host.
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        if (unmanagedDllName is "WebView2Loader.dll" or "WebView2Loader")
            return LoadUnmanagedDllFromPath(Path.Combine(_directory, "WebView2Loader.dll"));
        return IntPtr.Zero;
    }
}
