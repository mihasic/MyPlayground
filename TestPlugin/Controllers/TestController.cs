using System.Dynamic;
using System.Web.Mvc;

namespace TestPlugin.Controllers
{
    public class TestController : Controller
    {
         public ActionResult Index(string id, string text)
         {
             dynamic model = new ExpandoObject();
             model.Text = text ?? id;
             return View(model);
         }
    }
}