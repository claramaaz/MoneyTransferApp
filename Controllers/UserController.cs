using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;
using MoneyTransferApp.Repositories;
using MoneyTransferApp.Services;  // ← AJOUTER

namespace MoneyTransferApp.Controllers
{
    [Authorize(Roles = Roles.User)]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly TransactionRepository _txRepo;
        private readonly BeneficiaryRepository _benRepo;
        private readonly AgentRepository _agentRepo;
        private readonly IEmailService _email;  // ← AJOUTER

        public UserController(
            UserManager<User> userManager,
            ApplicationDbContext db,
            TransactionRepository txRepo,
            BeneficiaryRepository benRepo,
            AgentRepository agentRepo,
            IEmailService email)  // ← AJOUTER
        {
            _userManager = userManager;
            _db = db;
            _txRepo = txRepo;
            _benRepo = benRepo;
            _agentRepo = agentRepo;
            _email = email;  // ← AJOUTER
        }

        // ─────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            var recentTx = await _db.Transactions
                .Include(t => t.Sender)
                .Include(t => t.Recipient)
                .Where(t => t.SenderId == userId || t.RecipientId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.UnreadCount = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            ViewBag.RecentTx = recentTx;
            ViewBag.Notifications = notifications;
            ViewBag.TotalSent = await _db.Transactions.CountAsync(t => t.SenderId == userId);
            ViewBag.TotalReceived = await _db.Transactions.CountAsync(t => t.RecipientId == userId);

            return View(user);
        }

        // ─────────────────────────────────────────────────────────
        // TRANSFER
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Transfer()
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.Beneficiaries = await _db.Beneficiaries
                .Where(b => b.UserId == userId).ToListAsync();
            ViewBag.Currencies = await _db.Currencies
                .Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(
            string? recipientAccount, string? recipientPhone,
            string? recipientName, decimal amount,
            string fromCurrency, string toCurrency, string? note)
        {
            var userId = _userManager.GetUserId(User);
            var sender = await _db.Users.FindAsync(userId);
            if (sender == null) return RedirectToAction("Dashboard");

            if (amount <= 0)
            {
                TempData["Error"] = "Amount must be greater than 0.";
                return RedirectToAction("Transfer");
            }
            if (string.IsNullOrEmpty(recipientAccount) && string.IsNullOrEmpty(recipientPhone))
            {
                TempData["Error"] = "Please enter a recipient account number or phone number.";
                return RedirectToAction("Transfer");
            }

            decimal convertedAmount = amount;
            decimal rateUsed = 1m;
            if (fromCurrency != "USD")
            {
                var curr = await _db.Currencies.FindAsync(fromCurrency);
                if (curr != null && curr.ExchangeRate > 0)
                {
                    convertedAmount = Math.Round(amount / curr.ExchangeRate, 2);
                    rateUsed = curr.ExchangeRate;
                }
            }

            if (sender.Balance < convertedAmount)
            {
                TempData["Error"] = $"Insufficient balance. Available: {sender.Balance:F2} USD, Required: {convertedAmount:F2} USD";
                return RedirectToAction("Transfer");
            }

            User? recipient = null;
            if (!string.IsNullOrEmpty(recipientAccount))
            {
                recipient = await _db.Users.FirstOrDefaultAsync(u => u.AccountNumber == recipientAccount);
                if (recipient == null)
                {
                    TempData["Error"] = "Account not found: " + recipientAccount;
                    return RedirectToAction("Transfer");
                }
            }

            decimal commissionAmount = 0m;
            var tier = await _db.Commissions
                .Where(c => c.IsActive && c.MinAmount <= amount && c.MaxAmount >= amount)
                .FirstOrDefaultAsync();
            if (tier != null)
                commissionAmount = Math.Round(amount * tier.Rate / 100m, 2);

            var tx = new Transaction
            {
                SenderId = userId,
                RecipientId = recipient?.Id,
                RecipientPhone = recipientPhone,
                RecipientName = recipientName ?? recipient?.FullName,
                Amount = amount,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                ConvertedAmount = convertedAmount,
                ExchangeRateUsed = rateUsed,
                CommissionAmount = commissionAmount,
                Note = note,
                Status = TransactionStatus.Completed,
                Type = TransactionType.Transfer,
                CompletedAt = DateTime.Now
            };
            await _txRepo.AddAsync(tx);

            sender.Balance -= convertedAmount;
            await _userManager.UpdateAsync(sender);
            if (recipient != null)
            {
                recipient.Balance += convertedAmount;
                await _userManager.UpdateAsync(recipient);
            }

            // Notifications in-app
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = $"Transfer sent: {amount} {fromCurrency} = {convertedAmount:F2} USD. Serial: {tx.SerialNumber}",
                Type = "transfer",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            if (recipient != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = recipient.Id,
                    Message = $"Transfer received: {convertedAmount:F2} USD from {sender.FullName}. Serial: {tx.SerialNumber}",
                    Type = "transfer",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();

            // ── EMAILS ───────────────────────────────────────────
            // Email à l'expéditeur
            if (!string.IsNullOrEmpty(sender.Email))
                await _email.SendAsync(
                    sender.Email,
                    $"Transfer Confirmed — {tx.SerialNumber}",
                    EmailTemplates.TransferConfirmation(
                        sender.FullName, tx.SerialNumber,
                        amount, fromCurrency, convertedAmount, toCurrency));

            // Email au destinataire
            if (recipient != null && !string.IsNullOrEmpty(recipient.Email))
                await _email.SendAsync(
                    recipient.Email,
                    $"Transfer Received — {tx.SerialNumber}",
                    EmailTemplates.TransferConfirmation(
                        recipient.FullName, tx.SerialNumber,
                        amount, fromCurrency, convertedAmount, toCurrency));
            // ────────────────────────────────────────────────────

            TempData["Success"] = $"Transfer successful! Serial: {tx.SerialNumber}";
            return RedirectToAction("Transactions");
        }

        // ─────────────────────────────────────────────────────────
        // TRANSACTIONS
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Transactions()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _db.Transactions
                .Include(t => t.Sender)
                .Include(t => t.Recipient)
                .Where(t => t.SenderId == userId || t.RecipientId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        // ─────────────────────────────────────────────────────────
        // BENEFICIARIES
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Beneficiaries()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _benRepo.GetByUserIdAsync(userId);
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View(list);
        }

        [HttpGet]
        public IActionResult AddBeneficiary() => View(new Beneficiary());

        [HttpPost]
        public async Task<IActionResult> AddBeneficiary(Beneficiary model)
        {
            var userId = _userManager.GetUserId(User);
            model.UserId = userId;
            model.CreatedAt = DateTime.Now;
            await _benRepo.AddAsync(model);
            TempData["Success"] = "Beneficiary added successfully!";
            return RedirectToAction("Beneficiaries");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBeneficiary(int id)
        {
            await _benRepo.DeleteAsync(id);
            TempData["Success"] = "Beneficiary removed.";
            return RedirectToAction("Beneficiaries");
        }

        // ─────────────────────────────────────────────────────────
        // TOP UP
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> TopUp()
        {
            ViewBag.Currencies = await _db.Currencies
                .Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TopUp(decimal amount, string currency)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Dashboard");

            if (amount <= 0)
            {
                TempData["Error"] = "Amount must be greater than 0.";
                return RedirectToAction("TopUp");
            }

            decimal convertedAmount = amount;
            decimal rateUsed = 1m;
            if (currency != "USD")
            {
                var curr = await _db.Currencies.FindAsync(currency);
                if (curr != null && curr.ExchangeRate > 0)
                {
                    convertedAmount = Math.Round(amount / curr.ExchangeRate, 2);
                    rateUsed = curr.ExchangeRate;
                }
            }

            var tx = new Transaction
            {
                RecipientId = userId,
                Amount = amount,
                FromCurrency = currency,
                ToCurrency = "USD",
                ConvertedAmount = convertedAmount,
                ExchangeRateUsed = rateUsed,
                Status = TransactionStatus.Completed,
                Type = TransactionType.TopUp,
                CompletedAt = DateTime.Now
            };
            await _txRepo.AddAsync(tx);

            user.Balance += convertedAmount;
            await _userManager.UpdateAsync(user);

            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = $"Account topped up: {amount} {currency} = {convertedAmount:F2} USD. Serial: {tx.SerialNumber}",
                Type = "topup",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // ── EMAIL ────────────────────────────────────────────
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendAsync(
                    user.Email,
                    $"Top-Up Confirmed — {tx.SerialNumber}",
                    EmailTemplates.CashInConfirmation(
                        user.FullName, tx.SerialNumber,
                        amount, currency, convertedAmount));
            // ────────────────────────────────────────────────────

            TempData["Success"] = $"Account topped up with {convertedAmount:F2} USD!";
            return RedirectToAction("Dashboard");
        }

        // ─────────────────────────────────────────────────────────
        // AGENT MAP
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> AgentMap()
        {
            var agents = await _agentRepo.GetApprovedAgentsAsync();
            return View(agents);
        }

        // ─────────────────────────────────────────────────────────
        // REVIEWS
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Reviews()
        {
            var list = await _db.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User);
            var alreadyReviewed = await _db.Reviews
                .AnyAsync(r => r.UserId == userId && r.CreatedAt.Date == DateTime.Today);
            if (alreadyReviewed)
            {
                TempData["Error"] = "You already submitted a review today.";
                return RedirectToAction("Reviews");
            }
            _db.Reviews.Add(new Review
            {
                UserId = userId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("Reviews");
        }

        // ─────────────────────────────────────────────────────────
        // PROFILE
        // ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);
            ViewBag.Currencies = await _db.Currencies
                .Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string preferredCurrency)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Dashboard");
            user.PreferredCurrency = preferredCurrency;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Profile");
        }

        // ─────────────────────────────────────────────────────────
        // BECOME AN AGENT
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> BecomeAgent()
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (existing != null)
            {
                ViewBag.AlreadyApplied = true;
                ViewBag.AgentStatus = existing.Status;
            }
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyAsAgent(
            string storeName, string ownerName, string phone,
            string address, string workingHours,
            double latitude = 33.8938, double longitude = 35.5018)
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (existing != null)
            {
                TempData["Error"] = "You already have an agent application on file.";
                return RedirectToAction("BecomeAgent");
            }

            _db.Agents.Add(new Agent
            {
                UserId = userId,
                StoreName = storeName,
                OwnerName = ownerName,
                Phone = phone,
                Address = address,
                WorkingHours = workingHours,
                Latitude = latitude,
                Longitude = longitude,
                Status = AgentStatus.Pending,
                RegisteredAt = DateTime.Now
            });

            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = $"Your agent application for '{storeName}' has been submitted. Pending admin approval.",
                Type = "agent",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Application submitted! You will be notified once approved by admin.";
            return RedirectToAction("BecomeAgent");
        }

        // ─────────────────────────────────────────────────────────
        // MARK NOTIFICATIONS AS READ
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var userId = _userManager.GetUserId(User);
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }
    }
}