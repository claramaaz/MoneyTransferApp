// ============================================================
// Controllers/AdminController.cs — VERSION AVEC EMAILS
// Seules les méthodes ApproveAgent et RejectAgent sont modifiées
// Le reste est identique à ta version originale
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;
using MoneyTransferApp.Services;  // ← AJOUTER CE USING

namespace MoneyTransferApp.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;  // ← AJOUTER

        public AdminController(UserManager<User> userManager, ApplicationDbContext db,
            IEmailService email)  // ← AJOUTER IEmailService email
        {
            _userManager = userManager;
            _db = db;
            _email = email;  // ← AJOUTER
        }

        // ─────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();
            ViewBag.TotalAgents = await _db.Agents.CountAsync();
            ViewBag.PendingAgents = await _db.Agents.CountAsync(a => a.Status == AgentStatus.Pending);
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
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            return View(agents);
        }

        // ─────────────────────────────────────────────────────────
        // APPROVE AGENT — avec email
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ApproveAgent(int id)
        {
            var agent = await _db.Agents
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (agent == null) return NotFound();

            agent.Status = AgentStatus.Approved;
            agent.ApprovedAt = DateTime.Now;
            _db.Agents.Update(agent);

            var user = await _userManager.FindByIdAsync(agent.UserId);
            if (user != null)
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.User);
                if (!await _userManager.IsInRoleAsync(user, Roles.Agent))
                    await _userManager.AddToRoleAsync(user, Roles.Agent);

                // Notification in-app
                _db.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Message = $"Congratulations! Your agent application for '{agent.StoreName}' has been approved.",
                    Type = "agent",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                await _db.SaveChangesAsync();

                // ── EMAIL ────────────────────────────────────────
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _email.SendAsync(
                        user.Email,
                        "Agent Application Approved — Money Money",
                        EmailTemplates.AgentStatusUpdate(user.FullName, agent.StoreName, approved: true)
                    );
                }
                // ────────────────────────────────────────────────
            }
            else
            {
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = $"{agent.StoreName} has been approved!";
            return RedirectToAction("Agents");
        }

        // ─────────────────────────────────────────────────────────
        // REJECT AGENT — avec email
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RejectAgent(int id)
        {
            var agent = await _db.Agents
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (agent == null) return NotFound();

            agent.Status = AgentStatus.Rejected;
            _db.Agents.Update(agent);

            _db.Notifications.Add(new Notification
            {
                UserId = agent.UserId,
                Message = $"Your agent application for '{agent.StoreName}' was not approved at this time.",
                Type = "agent",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            // ── EMAIL ────────────────────────────────────────────
            var user = await _userManager.FindByIdAsync(agent.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _email.SendAsync(
                    user.Email,
                    "Agent Application Update — Money Money",
                    EmailTemplates.AgentStatusUpdate(user.FullName, agent.StoreName, approved: false)
                );
            }
            // ────────────────────────────────────────────────────

            TempData["Success"] = "Agent application rejected.";
            return RedirectToAction("Agents");
        }

        // ─────────────────────────────────────────────────────────
        // COMMISSIONS / CURRENCIES / REPORTS / USERS
        // (identiques à ta version originale — pas de changement)
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

        public async Task<IActionResult> Reports()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();
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

        public async Task<IActionResult> Users()
        {
            var list = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ChatProxy([FromBody] ChatRequest request)
        {
            var apiKey = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Anthropic:ApiKey"] ?? "";
            if (string.IsNullOrEmpty(apiKey))
                return Json(new { reply = "Chatbot not configured." });

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var systemPrompt = @"You are a helpful customer support assistant for Money Money. Be concise, friendly, and professional. Always respond in the same language the user writes in (French or English).";
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
            var reply = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "No response.";
            return Json(new { reply });
        }
    }

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