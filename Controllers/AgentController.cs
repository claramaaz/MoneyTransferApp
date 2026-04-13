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

        public AgentController(UserManager<User> userManager, AgentRepository agentRepo,
            TransactionRepository txRepo, ApplicationDbContext db)
        {
            _userManager = userManager;
            _agentRepo = agentRepo;
            _txRepo = txRepo;
            _db = db;
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

        // ─────────────────────────────────────────────────────────
        // CASH-IN
        // FIX 1 : SerialNumber supprimé ici — généré UNE SEULE FOIS
        //         dans TransactionRepository.AddAsync()
        // FIX 2 : Notification créée après la transaction (brief prof)
        // ─────────────────────────────────────────────────────────
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
            var tier = await _db.Commissions
                .Where(c => c.IsActive && c.MinAmount <= amount && c.MaxAmount >= amount)
                .FirstOrDefaultAsync();
            if (tier != null)
            {
                commissionAmount = Math.Round(amount * tier.Rate / 100m, 2);
                agent.TotalCommissionEarned += Math.Round(amount * tier.AgentShare / 100m, 2);
                _db.Agents.Update(agent);
            }

            // FIX 1 : PAS de SerialNumber ici — le Repository le génère dans AddAsync()
            var tx = new TxModel
            {
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
            // Après AddAsync, tx.SerialNumber est maintenant rempli par le Repository

            customer.Balance += convertedAmount;
            await _userManager.UpdateAsync(customer);

            // FIX 2 : Notification in-app pour le client (brief prof)
            _db.Notifications.Add(new Notification
            {
                UserId = customer.Id,
                Message = $"Cash-In received: {amount} {currency} = {convertedAmount:F2} USD. Serial: {tx.SerialNumber}",
                Type = "topup",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Cash-In OK! {tx.SerialNumber} | {amount} {currency} = {convertedAmount:F2} USD";
            return RedirectToAction("CashOperations");
        }

        // ─────────────────────────────────────────────────────────
        // CASH-OUT
        // FIX 1 : SerialNumber supprimé ici — généré dans le Repository
        // FIX 2 : Notification créée après la transaction
        // ─────────────────────────────────────────────────────────
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
                TempData["Error"] = $"Insufficient balance. Available: {customer.Balance:F2} USD, Required: {convertedAmount:F2} USD";
                return RedirectToAction("CashOperations");
            }

            decimal commissionAmount = 0m;
            var tier = await _db.Commissions
                .Where(c => c.IsActive && c.MinAmount <= amount && c.MaxAmount >= amount)
                .FirstOrDefaultAsync();
            if (tier != null)
            {
                commissionAmount = Math.Round(amount * tier.Rate / 100m, 2);
                agent.TotalCommissionEarned += Math.Round(amount * tier.AgentShare / 100m, 2);
                _db.Agents.Update(agent);
            }

            // FIX 1 : PAS de SerialNumber ici
            var tx = new TxModel
            {
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

            // FIX 2 : Notification pour le client
            _db.Notifications.Add(new Notification
            {
                UserId = customer.Id,
                Message = $"Cash-Out processed: {amount} {currency} = {convertedAmount:F2} USD debited. Serial: {tx.SerialNumber}",
                Type = "transfer",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Cash-Out OK! {tx.SerialNumber} | {amount} {currency} = {convertedAmount:F2} USD";
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
        // CHATBOT PROXY — Ollama local (gratuit)
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ChatProxy([FromBody] AgentChatRequest request)
        {
            var systemPrompt = "You are a helpful customer support assistant for Money Money, " +
                "a money transfer application. Help customers with: transactions (serial numbers TXN-...), " +
                "cash-in/cash-out, account balance (accounts ACC-...), currency conversion " +
                "(USD, EUR, LBP, AED, SAR, GBP), commission fees, agent locations and working hours. " +
                "Be concise, friendly, and professional. " +
                "Respond in the same language the user writes in (French or English).";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };
            foreach (var m in request.Messages)
                messages.Add(new { role = m.role, content = m.content });

            var body = new
            {
                model = "llama3.2",
                messages = messages,
                stream = false
            };

            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(60);

                var json = System.Text.Json.JsonSerializer.Serialize(body);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await http.PostAsync("http://localhost:11434/api/chat", content);
                var raw = await response.Content.ReadAsStringAsync();

                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var messageProp) &&
                    messageProp.TryGetProperty("content", out var contentProp))
                    return Json(new { reply = contentProp.GetString() ?? "Empty response." });

                if (root.TryGetProperty("response", out var responseProp))
                    return Json(new { reply = responseProp.GetString() ?? "Empty response." });

                if (root.TryGetProperty("error", out var errorProp))
                    return Json(new { reply = "⚠️ Ollama error: " + errorProp.GetString() });

                return Json(new { reply = "⚠️ Unexpected format: " + raw[..Math.Min(200, raw.Length)] });
            }
            catch (HttpRequestException)
            {
                return Json(new { reply = "⚠️ Ollama not running. Open a terminal and type: ollama serve" });
            }
            catch (Exception ex)
            {
                return Json(new { reply = "⚠️ Error: " + ex.Message });
            }
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