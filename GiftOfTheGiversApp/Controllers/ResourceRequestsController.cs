using Microsoft.AspNetCore.Mvc;

namespace GiftOfTheGiversApp.Controllers
{
    public class ResourceRequestsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
