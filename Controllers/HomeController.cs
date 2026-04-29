using Microsoft.AspNetCore.Mvc;
using MoneyTransferApp.Models;
using System.Diagnostics;

namespace MoneyTransferApp.Controllers
{
    public class HomeController : Controller
    {
        // ─────────────────────────────────────────────────────────
        // Page d'accueil — choix du rôle
        // Accessible à tous (pas de [Authorize])
        // ─────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            // Si déjà connecté → rediriger directement vers le bon dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Agent")) return RedirectToAction("Dashboard", "Agent");
                return RedirectToAction("Dashboard", "User");
            }

            return View();
        }

        // Page d'erreur (générée par défaut par Visual Studio)
        public IActionResult Error()
        {
            return View();
        }
    }
}

