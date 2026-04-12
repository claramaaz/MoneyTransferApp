using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<User> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // ─────────────────────────────────────────────────────────
        // DASHBOARD
        // BUG FIX : TotalVolume utilise ConvertedAmount (USD équivalent)
        //           au lieu de Amount brut (qui inclut LBP/EUR sans conversion)
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();
            ViewBag.TotalAgents = await _db.Agents.CountAsync();
            ViewBag.PendingAgents = await _db.Agents.CountAsync(a => a.Status == AgentStatus.Pending);

            // BUG FIX : utiliser ConvertedAmount (toujours en USD) pour le volume réel
            ViewBag.TotalVolume = Math.Round(
                await _db.Transactions.SumAsync(t => (decimal?)t.ConvertedAmount) ?? 0, 2);

            var recentTx = await _db.Transactions
                .Include(t => t.Sender)
                .Include(t => t.Recipient)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(recentTx);
        }

        // ─────────────────────────────────────────────────────────
        // AGENTS
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Agents()
        {
            var agents = await _db.Agents
                .Include(a => a.User)
                .OrderByDescending(a => a.RegisteredAt)
                .ToListAsync();
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View(agents);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAgent(int id)
        {
            var agent = await _db.Agents.FindAsync(id);
            if (agent == null) return NotFound();
            agent.Status = AgentStatus.Approved;
            agent.ApprovedAt = DateTime.Now;
            _db.Agents.Update(agent);
            await _db.SaveChangesAsync();
            TempData["Success"] = agent.StoreName + " has been approved!";
            return RedirectToAction("Agents");
        }

        [HttpPost]
        public async Task<IActionResult> RejectAgent(int id)
        {
            var agent = await _db.Agents.FindAsync(id);
            if (agent == null) return NotFound();
            agent.Status = AgentStatus.Rejected;
            _db.Agents.Update(agent);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Agent rejected.";
            return RedirectToAction("Agents");
        }

        // ─────────────────────────────────────────────────────────
        // COMMISSIONS
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Commissions()
        {
            var list = await _db.Commissions.OrderBy(c => c.MinAmount).ToListAsync();
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View(list);
        }

        [HttpGet]
        public IActionResult AddCommission() => View(new Commission());

        [HttpPost]
        public async Task<IActionResult> AddCommission(Commission model)
        {
            model.UpdatedAt = DateTime.Now;
            model.PlatformShare = Math.Round(model.Rate - model.AgentShare, 2);
            _db.Commissions.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Commission tier added!";
            return RedirectToAction("Commissions");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCommission(int id, decimal agentShare)
        {
            var existing = await _db.Commissions.FindAsync(id);
            if (existing == null) return NotFound();
            existing.AgentShare = agentShare;
            existing.PlatformShare = Math.Round(existing.Rate - agentShare, 2);
            existing.UpdatedAt = DateTime.Now;
            _db.Commissions.Update(existing);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Commission updated!";
            return RedirectToAction("Commissions");
        }

        // ─────────────────────────────────────────────────────────
        // CURRENCIES
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Currencies()
        {
            var list = await _db.Currencies.OrderBy(c => c.Code).ToListAsync();
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCurrency(string code, decimal exchangeRate, bool isActive = true)
        {
            var existing = await _db.Currencies.FindAsync(code);
            if (existing == null) return NotFound();
            existing.ExchangeRate = exchangeRate;
            existing.IsActive = isActive;
            existing.LastUpdated = DateTime.Now;
            _db.Currencies.Update(existing);
            await _db.SaveChangesAsync();
            TempData["Success"] = code + " updated!";
            return RedirectToAction("Currencies");
        }

        // ─────────────────────────────────────────────────────────
        // REPORTS
        // BUG FIX : TotalVolume utilise ConvertedAmount
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Reports()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();

            // BUG FIX : ConvertedAmount pour le vrai volume en USD
            ViewBag.TotalVolume = Math.Round(
                await _db.Transactions.SumAsync(t => (decimal?)t.ConvertedAmount) ?? 0, 2);

            ViewBag.AverageRating = Math.Round(
                await _db.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0, 1);

            ViewBag.TopAgents = await _db.Agents
                .Where(a => a.Status == AgentStatus.Approved)
                .OrderByDescending(a => a.TotalCommissionEarned)
                .Take(5)
                .ToListAsync();

            var reviews = await _db.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(reviews);
        }

        // ─────────────────────────────────────────────────────────
        // USERS
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Users()
        {
            var list = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
            return View(list);
        }

        // ─────────────────────────────────────────────────────────
        // CHATBOT PROXY — BUG FIX chatbot
        // L'appel API Anthropic se fait ICI côté serveur (pas depuis le browser)
        // car l'API key ne peut pas être exposée côté client
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ChatProxy([FromBody] ChatRequest request)
        {
            // Récupère la clé depuis appsettings.json → "Anthropic:ApiKey"
            var apiKey = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Anthropic:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(apiKey))
                return Json(new { reply = "Chatbot not configured. Please contact your administrator." });

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var systemPrompt = @"You are a helpful customer support assistant for Money Money, 
a money transfer application. You help users with:
- Money transfers and transaction status
- Cash-in and cash-out operations at agent stores  
- Account balance (accounts are in USD equivalent)
- Transaction serial numbers (format: TXN-YYYYMMDD-XXXXXX)
- Currency conversion (USD, EUR, LBP, AED, SAR, GBP)
- Commission fees on transactions
- Finding agent locations and working hours
Be concise, friendly, and professional. 
If you need specific transaction details, ask for the serial number.
Always respond in the same language the user writes in (French or English).";

            var body = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 500,
                system = systemPrompt,
                messages = request.Messages
            };

            var json = System.Text.Json.JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://api.anthropic.com/v1/messages", content);
            var raw = await response.Content.ReadAsStringAsync();

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            var reply = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "No response.";

            return Json(new { reply });
        }
    }

    // ─────────────────────────────────────────────────────────
    // Modèle pour le ChatProxy
    // ─────────────────────────────────────────────────────────
    public class ChatRequest
    {
        public List<ChatMessage> Messages { get; set; } = new();
    }

    public class ChatMessage
    {
        public string role { get; set; } = "";
        public string content { get; set; } = "";
    }
}
