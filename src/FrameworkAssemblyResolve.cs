#if NETFRAMEWORK
using System.Reflection;
using System.Runtime.CompilerServices;

namespace iSurvey;

/// <summary>
/// AutoCAD 2023 (.NET Framework) không dò DLL phụ trong thư mục plugin. Nạp ProjNET và System.Text.Json từ cùng thư mục.
/// </summary>
internal static class FrameworkAssemblyResolve
{
    [ModuleInitializer]
    internal static void Register()
    {
        var dir = Path.GetDirectoryName(typeof(FrameworkAssemblyResolve).Assembly.Location);
        if (string.IsNullOrEmpty(dir))
            return;

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            try
            {
                var simple = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(simple))
                    return null;

                foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(loaded.GetName().Name, simple, StringComparison.OrdinalIgnoreCase))
                        return loaded;
                }

                var path = Path.Combine(dir, simple + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            }
            catch
            {
                return null;
            }
        };
    }
}
#endif
