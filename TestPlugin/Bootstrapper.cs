using System.Web.Mvc;
using System.Web.Routing;

namespace TestPlugin
{
    public static class Bootstrapper
    {
        public static void Init()
        {
            RouteTable.Routes.MapRoute(
                name: "TestPlugin",
                url: "Test/{id}",
                defaults: new { controller = "Test", action = "Index", id = UrlParameter.Optional, area = "TestPlugin.dll" }
            ).DataTokens.Add("area", "TestPlugin.dll");
        }
    }
}
