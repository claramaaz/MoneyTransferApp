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
            ViewBag.Transactions = transactions;
            ViewBag.TodayCashIn = transactions
                .Where(t => t.Type == TransactionType.CashIn && t.CreatedAt.Date == DateTime.Today)
                .Sum(t => t.Amount);
            ViewBag.TodayCashOut = transactions
                .Where(t => t.Type == TransactionType.CashOut && t.CreatedAt.Date == DateTime.Today)
                .Sum(t => t.Amount);
            ViewBag.Commission = agent.TotalCommissionEarned;

            return View(agent);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new Agent());
        }

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

        [HttpGet]
        public IActionResult CashOperations()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CashIn(string customerAccount, decimal amount, string currency)
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToAction("Dashboard");

            var customer = await _db.Users.FirstOrDefaultAsync(u => u.AccountNumber == customerAccount);

            var tx = new TxModel
            {
                AgentId = agent.Id,
                RecipientId = customer?.Id,
                Amount = amount,
                FromCurrency = currency,
                ToCurrency = currency,
                ConvertedAmount = amount,
                Status = TransactionStatus.Completed,
                Type = TransactionType.CashIn,
                CompletedAt = DateTime.Now
            };

            await _txRepo.AddAsync(tx);

            if (customer != null)
            {
                customer.Balance += amount;
                await _userManager.UpdateAsync(customer);
            }

            TempData["Success"] = "Cash-In processed! Serial: " + tx.SerialNumber;
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CashOut(string customerAccount, decimal amount, string currency)
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToAction("Dashboard");

            var customer = await _db.Users.FirstOrDefaultAsync(u => u.AccountNumber == customerAccount);

            var tx = new TxModel
            {
                AgentId = agent.Id,
                SenderId = customer?.Id,
                Amount = amount,
                FromCurrency = currency,
                ToCurrency = currency,
                ConvertedAmount = amount,
                Status = TransactionStatus.Completed,
                Type = TransactionType.CashOut,
                CompletedAt = DateTime.Now
            };

            await _txRepo.AddAsync(tx);

            if (customer != null && customer.Balance >= amount)
            {
                customer.Balance -= amount;
                await _userManager.UpdateAsync(customer);
            }

            TempData["Success"] = "Cash-Out processed! Serial: " + tx.SerialNumber;
            return RedirectToAction("Dashboard");
        }

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
            var list = await _db.Transactions
                .Where(t => t.AgentId == agent!.Id && t.CommissionAmount > 0)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userId = _userManager.GetUserId(User);
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            return View(agent);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(Agent model)
        {
            var existing = await _db.Agents.FindAsync(model.Id);
            if (existing == null) return NotFound();

            existing.StoreName = model.StoreName;
            existing.Address = model.Address;
            existing.Phone = model.Phone;
            existing.Latitude = model.Latitude;
            existing.Longitude = model.Longitude;
            existing.WorkingHours = model.WorkingHours;

            _db.Agents.Update(existing);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Settings updated!";
            return RedirectToAction("Settings");
        }
    }
}

