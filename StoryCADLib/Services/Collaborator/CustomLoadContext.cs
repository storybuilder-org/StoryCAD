using System.Reflection;
using System.Runtime.Loader;

namespace StoryCADLib.Services.Collaborator;

public class CustomLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;

    public CustomLoadContext(string basePath)
    {
        _basePath = basePath;
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = Path.Combine(_basePath, $"{assemblyName.Name}.dll");
        if (File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null; // Fallback to default context
    }
}
