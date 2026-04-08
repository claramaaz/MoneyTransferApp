using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                        return RedirectToAction("Dashboard", "Admin");
                    if (await _userManager.IsInRoleAsync(user, Roles.Agent))
                        return RedirectToAction("Dashboard", "Agent");
                    return RedirectToAction("Dashboard", "User");
                }
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string preferredCurrency)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                PreferredCurrency = preferredCurrency,
                AccountNumber = "ACC-" + DateTime.Now.Year + "-" + new Random().Next(10000, 99999),
                Balance = 0
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.User);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Dashboard", "User");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Auth");
        }
    }
}
