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

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();
            ViewBag.TotalAgents = await _db.Agents.CountAsync();
            ViewBag.PendingAgents = await _db.Agents.CountAsync(a => a.Status == AgentStatus.Pending);
            ViewBag.TotalVolume = await _db.Transactions.SumAsync(t => (decimal?)t.Amount) ?? 0;

            var recentTx = await _db.Transactions
                .Include(t => t.Sender)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentTx);
        }

        public async Task<IActionResult> Agents()
        {
            var agents = await _db.Agents
                .Include(a => a.User)
                .OrderByDescending(a => a.RegisteredAt)
                .ToListAsync();
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

        public async Task<IActionResult> Commissions()
        {
            var list = await _db.Commissions.ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult AddCommission()
        {
            return View(new Commission());
        }

        [HttpPost]
        public async Task<IActionResult> AddCommission(Commission model)
        {
            model.UpdatedAt = DateTime.Now;
            _db.Commissions.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Commission tier added!";
            return RedirectToAction("Commissions");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCommission(Commission model)
        {
            model.UpdatedAt = DateTime.Now;
            _db.Commissions.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Commission updated!";
            return RedirectToAction("Commissions");
        }

        public async Task<IActionResult> Currencies()
        {
            var list = await _db.Currencies.ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCurrency(string code, decimal exchangeRate, bool isActive)
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
            ViewBag.TotalVolume = await _db.Transactions.SumAsync(t => (decimal?)t.Amount) ?? 0;
            ViewBag.AverageRating = await _db.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0;
            ViewBag.Reviews = await _db.Reviews.Include(r => r.User)
                                            .OrderByDescending(r => r.CreatedAt).Take(5).ToListAsync();
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var list = await _db.Users.ToListAsync();
            return View(list);
        }
    }
}