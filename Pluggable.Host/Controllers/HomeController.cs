using System.Web.Mvc;

namespace Pluggable.Host.Controllers
{
    public class HomeController : Controller
    {
         public ActionResult Index()
         {
             return View();
         }
    }
}