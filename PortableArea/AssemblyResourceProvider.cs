using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;

namespace PortableArea
{
    internal class AssemblyResourceProvider : VirtualPathProvider
    {
        private static bool IsAppResourcePath(string virtualPath)
        {
            var checkPath = VirtualPathUtility.ToAppRelative(virtualPath);
            return checkPath.StartsWith("~/Areas/", StringComparison.OrdinalIgnoreCase);
        }

        private readonly ConcurrentDictionary<string, Tuple<string, Assembly>> _cache =
            new ConcurrentDictionary<string, Tuple<string, Assembly>>(StringComparer.OrdinalIgnoreCase);

        public override bool FileExists(string virtualPath)
        {
            if (IsAppResourcePath(virtualPath))
            {
                return !string.IsNullOrEmpty(GetResourceDefinition(virtualPath).Item1);
            }
            return base.FileExists(virtualPath);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (IsAppResourcePath(virtualPath))
                return new AssemblyResourceVirtualFile(virtualPath, this);
            return base.GetFile(virtualPath);
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (IsAppResourcePath(virtualPath))
                return null;

            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        private Tuple<string, Assembly> GetResourceDefinition(string path)
        {
            path = VirtualPathUtility.ToAppRelative(path);

            return _cache.GetOrAdd(path, k =>
            {
                var parts = k.Split('/');
                var assemblyName = parts[2];

                var extIndex = assemblyName.LastIndexOf(".dll", StringComparison.OrdinalIgnoreCase);
                if (extIndex <= 0)
                    return Tuple.Create<string, Assembly>(null, null);

                var resourceName = Path.GetFileNameWithoutExtension(assemblyName) + "." + string.Join(".", parts.Skip(3));

                assemblyName = Path.Combine(Path.Combine(HttpRuntime.BinDirectory, "..\\Plugins"), assemblyName);
                var assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyName));
                if (assembly != null)
                {
                    return Tuple.Create(assembly.GetManifestResourceNames()
                                                .SingleOrDefault(r => r.Equals(resourceName, StringComparison.OrdinalIgnoreCase)),
                                        assembly);
                }
                return Tuple.Create<string, Assembly>(null, null);
            });
        }

        class AssemblyResourceVirtualFile : VirtualFile
        {
            private readonly string _path;
            private readonly AssemblyResourceProvider _provider;

            public AssemblyResourceVirtualFile(string virtualPath, AssemblyResourceProvider provider)
                : base(virtualPath)
            {
                _path = virtualPath;
                _provider = provider;
            }

            public override Stream Open()
            {
                var res = _provider.GetResourceDefinition(_path);
                var stream = !string.IsNullOrEmpty(res.Item1) ? res.Item2.GetManifestResourceStream(res.Item1) : null;
                return stream;
            }
        }
    }
}