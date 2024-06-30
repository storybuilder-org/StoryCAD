using System.Reflection;
using System.Runtime.Loader;

namespace StoryCAD.Services.Collaborator
{
    public class CustomLoadContext : AssemblyLoadContext
    {
        private string _basePath;

        public CustomLoadContext(string basePath)
        {
            _basePath = basePath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = Path.Combine(_basePath, $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyPath))
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null; // Fallback to default context
        }
    }
}
