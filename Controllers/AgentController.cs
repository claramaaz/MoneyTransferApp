using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;
using MoneyTransferApp.Repositories;

using TxModel = MoneyTransferApp.Models.Transaction;

namespace MoneyTransferApp.Controllers
{
    [Authorize(Roles = Roles.Agent)]
    public class AgentController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AgentRepository _agentRepo;
        private readonly TransactionRepository _txRepo;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AgentController(UserManager<User> userManager, AgentRepository agentRepo,
            TransactionRepository txRepo, ApplicationDbContext db, IConfiguration config)
        {
            _userManager = userManager;
            _agentRepo = agentRepo;
            _txRepo = txRepo;
            _db = db;
            _config = config;
        }

        // ─────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToAction("Register");

            var transactions = await _db.Transactions
                .Where(t => t.AgentId == agent.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Agent = agent;
            ViewBag.TodayTransactions = transactions.Count(t => t.CreatedAt.Date == DateTime.Today);
            ViewBag.TodayVolume = Math.Round(transactions
                .Where(t => t.CreatedAt.Date == DateTime.Today)
                .Sum(t => t.ConvertedAmount), 2);
            ViewBag.TodayCommission = Math.Round(transactions
                .Where(t => t.CreatedAt.Date == DateTime.Today)
                .Sum(t => t.CommissionAmount), 2);
            ViewBag.ActiveCustomers = transactions
                .Where(t => t.CreatedAt >= DateTime.Today.AddDays(-30))
                .Select(t => t.SenderId ?? t.RecipientId)
                .Distinct().Count();

            return View(transactions);
        }

        // ─────────────────────────────────────────────────────────
        // REGISTER AS AGENT
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View(new Agent());

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(Agent model)
        {
            var userId = _userManager.GetUserId(User);
            model.UserId = userId ?? "";
            model.Status = AgentStatus.Pending;
            model.RegisteredAt = DateTime.Now;
            await _agentRepo.AddAsync(model);
            TempData["Success"] = "Application submitted. Waiting for admin approval.";
            return RedirectToAction("Login", "Auth");
        }

        // ─────────────────────────────────────────────────────────
        // CASH OPERATIONS
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult CashOperations()
        {
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CashIn(string customerAccount, decimal amount, string currency)
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToAction("Dashboard");

            var customer = await _db.Users
                .FirstOrDefaultAsync(u => u.AccountNumber == customerAccount);
            if (customer == null)
            {
                TempData["Error"] = "Account not found: " + customerAccount;
                return RedirectToAction("CashOperations");
            }

            // Conversion devise → USD
            decimal convertedAmount = amount;
            decimal rateUsed = 1m;
            if (currency != "USD")
            {
                var currencyRecord = await _db.Currencies.FindAsync(currency);
                if (currencyRecord != null && currencyRecord.ExchangeRate > 0)
                {
                    convertedAmount = Math.Round(amount / currencyRecord.ExchangeRate, 2);
                    rateUsed = currencyRecord.ExchangeRate;
                }
            }

            // Commission
            decimal commissionAmount = 0m;
            var commissionTier = await _db.Commissions
                .Where(c => c.IsActive && c.MinAmount <= amount && c.MaxAmount >= amount)
                .FirstOrDefaultAsync();
            if (commissionTier != null)
            {
                commissionAmount = Math.Round(amount * commissionTier.Rate / 100m, 2);
                agent.TotalCommissionEarned += Math.Round(amount * commissionTier.AgentShare / 100m, 2);
                _db.Agents.Update(agent);
            }

            var serial = "TXN-" + DateTime.Now.ToString("yyyyMMdd") + "-" +
                         Guid.NewGuid().ToString("N")[..6].ToUpper();

            var tx = new TxModel
            {
                SerialNumber = serial,
                AgentId = agent.Id,
                RecipientId = customer.Id,
                Amount = amount,
                FromCurrency = currency,
                ToCurrency = "USD",
                ConvertedAmount = convertedAmount,
                ExchangeRateUsed = rateUsed,
                CommissionAmount = commissionAmount,
                Status = TransactionStatus.Completed,
                Type = TransactionType.CashIn,
                CompletedAt = DateTime.Now
            };

            await _txRepo.AddAsync(tx);
            customer.Balance += convertedAmount;
            await _userManager.UpdateAsync(customer);

            TempData["Success"] = $"Cash-In OK! {serial} | {amount} {currency} = ${convertedAmount} USD";
            return RedirectToAction("CashOperations");
        }

        [HttpPost]
        public async Task<IActionResult> CashOut(string customerAccount, decimal amount, string currency)
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToAction("Dashboard");

            var customer = await _db.Users
                .FirstOrDefaultAsync(u => u.AccountNumber == customerAccount);
            if (customer == null)
            {
                TempData["Error"] = "Account not found: " + customerAccount;
                return RedirectToAction("CashOperations");
            }

            decimal convertedAmount = amount;
            decimal rateUsed = 1m;
            if (currency != "USD")
            {
                var currencyRecord = await _db.Currencies.FindAsync(currency);
                if (currencyRecord != null && currencyRecord.ExchangeRate > 0)
                {
                    convertedAmount = Math.Round(amount / currencyRecord.ExchangeRate, 2);
                    rateUsed = currencyRecord.ExchangeRate;
                }
            }

            if (customer.Balance < convertedAmount)
            {
                TempData["Error"] = $"Insufficient balance. Available: ${customer.Balance:F2}, Required: ${convertedAmount:F2}";
                return RedirectToAction("CashOperations");
            }

            decimal commissionAmount = 0m;
            var commissionTier = await _db.Commissions
                .Where(c => c.IsActive && c.MinAmount <= amount && c.MaxAmount >= amount)
                .FirstOrDefaultAsync();
            if (commissionTier != null)
            {
                commissionAmount = Math.Round(amount * commissionTier.Rate / 100m, 2);
                agent.TotalCommissionEarned += Math.Round(amount * commissionTier.AgentShare / 100m, 2);
                _db.Agents.Update(agent);
            }

            var serial = "TXN-" + DateTime.Now.ToString("yyyyMMdd") + "-" +
                         Guid.NewGuid().ToString("N")[..6].ToUpper();

            var tx = new TxModel
            {
                SerialNumber = serial,
                AgentId = agent.Id,
                SenderId = customer.Id,
                Amount = amount,
                FromCurrency = currency,
                ToCurrency = "USD",
                ConvertedAmount = convertedAmount,
                ExchangeRateUsed = rateUsed,
                CommissionAmount = commissionAmount,
                Status = TransactionStatus.Completed,
                Type = TransactionType.CashOut,
                CompletedAt = DateTime.Now
            };

            await _txRepo.AddAsync(tx);
            customer.Balance -= convertedAmount;
            await _userManager.UpdateAsync(customer);

            TempData["Success"] = $"Cash-Out OK! {serial} | {amount} {currency} = ${convertedAmount} USD";
            return RedirectToAction("CashOperations");
        }

        // ─────────────────────────────────────────────────────────
        // TRANSACTIONS / COMMISSIONS
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Transactions()
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            var list = await _db.Transactions
                .Where(t => t.AgentId == agent!.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Commissions()
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            ViewBag.Agent = agent;
            ViewBag.TotalCommission = Math.Round(agent?.TotalCommissionEarned ?? 0, 2);
            var list = await _db.Transactions
                .Where(t => t.AgentId == agent!.Id && t.CommissionAmount > 0)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        // ─────────────────────────────────────────────────────────
        // SETTINGS
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View(agent);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(int id, string storeName, string address,
            string? phone, string? workingHours, double latitude, double longitude)
        {
            var existing = await _db.Agents.FindAsync(id);
            if (existing == null) return NotFound();
            existing.StoreName = storeName;
            existing.Address = address;
            existing.Phone = phone;
            existing.WorkingHours = workingHours;
            existing.Latitude = latitude;
            existing.Longitude = longitude;
            _db.Agents.Update(existing);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Settings updated successfully!";
            return RedirectToAction("Settings");
        }

        // ─────────────────────────────────────────────────────────
        // SUPPORT
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Support()
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            ViewBag.Agent = agent;
            return View();
        }

        // ─────────────────────────────────────────────────────────
        // CHATBOT PROXY — appel serveur → Anthropic API
        // BUG FIX chatbot : l'appel passe par le serveur ASP.NET
        // pour éviter le blocage CORS du browser
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ChatProxy([FromBody] AgentChatRequest request)
        {
            var apiKey = _config["Anthropic:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(apiKey))
                return Json(new { reply = "Chatbot not configured. Please add Anthropic:ApiKey to appsettings.json." });

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var systemPrompt = @"You are a helpful customer support assistant for Money Money, 
a money transfer application. You help customers with:
- Transaction status (serial numbers like TXN-YYYYMMDD-XXXXXX)
- Cash-in and cash-out operations
- Account balance and account numbers (ACC-YYYY-XXXXX)
- Currency conversion (USD, EUR, LBP, AED, SAR, GBP)
- Commission fees
- Agent store working hours and locations
Be concise, friendly, and professional.
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

    public class AgentChatRequest
    {
        public List<AgentChatMessage> Messages { get; set; } = new();
    }

    public class AgentChatMessage
    {
        public string role { get; set; } = "";
        public string content { get; set; } = "";
    }
}
