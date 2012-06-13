using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;

[assembly: PreApplicationStartMethod(typeof(PortableArea.PreApplicationInit), "Initialize")]

namespace PortableArea
{
    public static class PreApplicationInit
    {
        public static void Initialize()
        {
            var pluginContainers = new DirectoryInfo(HostingEnvironment.MapPath("~/Plugins"))
                .GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                .Select(f => new
                {
                    //Assembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(f.FullName)),
                    Assembly = Assembly.Load(AssemblyName.GetAssemblyName(f.FullName)),
                    Name = f.Name
                }).ToList();
            pluginContainers.ForEach(pc => BuildManager.AddReferencedAssembly(pc.Assembly));
            pluginContainers.ForEach(pc => Register(pc.Name, pc.Assembly));

            HostingEnvironment.RegisterVirtualPathProvider(new AssemblyResourceProvider());

            pluginContainers.SelectMany(pc => pc.Assembly.GetTypes())
                            .Where(t => "Bootstrapper".Equals(t.Name, StringComparison.OrdinalIgnoreCase))
                            .Select(t => t.GetMethod("Init", BindingFlags.Static | BindingFlags.Public))
                            .Where(mi => mi != null && !mi.GetParameters().Any() && !mi.IsGenericMethodDefinition)
                            .Select(mi => mi.Invoke(null, null))
                            .ToList();
        }

        public static void Register(string name, Assembly assembly)
        {
            var namespaces = assembly.GetTypes()
                                     .Where(t => typeof(Controller).IsAssignableFrom(t))
                                     .Select(t => t.Namespace).ToArray();

            var route = RouteTable.Routes.MapRoute(name,
                                                   string.Format(CultureInfo.InvariantCulture, "Plugin/{0}/{{controller}}/{{action}}/{{id}}", name),
                                                   new { action = "Index", id = UrlParameter.Optional },
                                                   null,
                                                   namespaces);
            route.DataTokens["area"] = name;
            var flag = namespaces.Length == 0;
            route.DataTokens["UseNamespaceFallback"] = flag;
        }
    }
}
