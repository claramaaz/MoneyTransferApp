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

        public AuthController(UserManager<User> userManager,
                              SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ─────────────────────────────────────────────────────────
        // LANDING PAGE — redirige vers Home/Index
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────────────────────────
        // LOGIN GET
        // Reçoit le paramètre "role" depuis la page d'accueil
        // ex: /Auth/Login?role=Admin
        // Fidèle au cours PDF 10 : SignInManager
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? role)
        {
            // Si déjà connecté → rediriger directement
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(Roles.Admin)) return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole(Roles.Agent)) return RedirectToAction("Dashboard", "Agent");
                return RedirectToAction("Dashboard", "User");
            }

            // Passer le rôle choisi à la vue (pour l'affichage du badge)
            ViewBag.Role = role ?? "";
            return View();
        }

        // ─────────────────────────────────────────────────────────
        // LOGIN POST
        // Cours PDF 10 : PasswordSignInAsync + IsInRoleAsync
        // Le paramètre "role" est utilisé pour la redirection
        // si l'utilisateur s'est connecté depuis la mauvaise page
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password,
                                               bool rememberMe, string? role)
        {
            // PDF 10 : SignInManager.PasswordSignInAsync
            var result = await _signInManager.PasswordSignInAsync(
                email, password, rememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // PDF 10 : IsInRoleAsync — redirection selon le rôle réel
                    if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                        return RedirectToAction("Dashboard", "Admin");

                    if (await _userManager.IsInRoleAsync(user, Roles.Agent))
                        return RedirectToAction("Dashboard", "Agent");

                    return RedirectToAction("Dashboard", "User");
                }
            }

            // Mauvais mot de passe ou email inexistant
            ViewBag.Error = "Invalid email or password. Please try again.";
            ViewBag.Role = role ?? "";
            return View();
        }

        // ─────────────────────────────────────────────────────────
        // REGISTER GET
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Register() => View();

        // ─────────────────────────────────────────────────────────
        // REGISTER POST
        // Cours PDF 10 : UserManager.CreateAsync + AddToRoleAsync
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email,
                                                  string password, string preferredCurrency)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                PreferredCurrency = preferredCurrency,
                AccountNumber = "ACC-" + DateTime.Now.Year + "-" +
                                    new Random().Next(10000, 99999),
                Balance = 0
            };

            // PDF 10 : UserManager.CreateAsync
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // PDF 10 : AddToRoleAsync — nouveau user = rôle User
                await _userManager.AddToRoleAsync(user, Roles.User);
                // PDF 10 : SignInAsync après inscription
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Dashboard", "User");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        // ─────────────────────────────────────────────────────────
        // LOGOUT
        // Cours PDF 10 : SignOutAsync
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // PDF 10 : SignInManager.SignOutAsync
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
