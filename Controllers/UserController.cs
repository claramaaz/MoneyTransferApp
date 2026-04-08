using Microsoft.AspNetCore.Mvc;

namespace MoneyTransferApp.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
