// ============================================================
// Controllers/Api/TransactionApiController.cs
// RESTful API — fidèle au cours PDF 12 (Ali Ibrahim)
//
// Structure exactement comme montré dans le cours :
//   [ApiController]
//   [Route("[controller]")]
//   public class XxxController : ControllerBase
//   + méthodes HttpGet, HttpPost, HttpPut, HttpDelete
//   + Swagger pour tester (visible sur /swagger/index.html)
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Controllers.Api
{
    // ── Exactement comme le cours slide 25 ──────────────────
    [ApiController]
    [Route("api/[controller]")]   // → accès via /api/TransactionApi
    public class TransactionApiController : ControllerBase
    {
        // ControllerBase = pas de View support (cours slide 25)
        private readonly ApplicationDbContext _context;

        public TransactionApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── GET /api/TransactionApi ──────────────────────────
        // Cours slide 35: [HttpGet(Name = "GetAll")]
        [HttpGet(Name = "GetAllTransactions")]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var transactions = await _context.Transactions
                .Select(t => new
                {
                    t.Id,
                    t.SerialNumber,
                    t.Amount,
                    t.FromCurrency,
                    t.ToCurrency,
                    t.ConvertedAmount,
                    t.Status,
                    t.Type,
                    t.CreatedAt
                })
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(transactions);
        }

        // ── GET /api/TransactionApi/{id} ─────────────────────
        // Cours slide 35: [HttpGet("{id}")]
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var t = await _context.Transactions
                .Include(x => x.Sender)
                .Include(x => x.Recipient)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t == null)
                return NotFound();   // cours slide 35 : return NotFound()

            return Ok(new
            {
                t.Id,
                t.SerialNumber,
                t.Amount,
                t.FromCurrency,
                t.ToCurrency,
                t.ConvertedAmount,
                t.ExchangeRateUsed,
                t.CommissionAmount,
                t.Status,
                t.Type,
                t.Note,
                t.CreatedAt,
                Sender = t.Sender?.FullName,
                Recipient = t.Recipient?.FullName ?? t.RecipientName ?? t.RecipientPhone
            });
        }

        // ── GET /api/TransactionApi/user/{userId} ─────────────
        // Requête personnalisée : transactions d'un user spécifique
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetByUser(string userId)
        {
            var list = await _context.Transactions
                .Where(t => t.SenderId == userId || t.RecipientId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.SerialNumber,
                    t.Amount,
                    t.FromCurrency,
                    t.ConvertedAmount,
                    t.Status,
                    t.Type,
                    t.CreatedAt
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }

        // ── GET /api/TransactionApi/serial/{serial} ───────────
        // Tracking par numéro de série (brief prof)
        [HttpGet("serial/{serial}")]
        public async Task<ActionResult<object>> GetBySerial(string serial)
        {
            var t = await _context.Transactions
                .Include(x => x.Sender)
                .Include(x => x.Recipient)
                .FirstOrDefaultAsync(x => x.SerialNumber == serial);

            if (t == null)
                return NotFound(new { message = "Transaction not found: " + serial });

            return Ok(new
            {
                t.SerialNumber,
                t.Amount,
                t.FromCurrency,
                t.ToCurrency,
                t.ConvertedAmount,
                t.Status,
                t.Type,
                t.CreatedAt,
                t.CompletedAt,
                Sender = t.Sender?.FullName ?? "—",
                Recipient = t.Recipient?.FullName ?? t.RecipientName ?? t.RecipientPhone ?? "—"
            });
        }

        // ── GET /api/TransactionApi/agents ────────────────────
        // Tous les agents approuvés (pour la carte)
        [HttpGet("agents")]
        public async Task<ActionResult<IEnumerable<object>>> GetAgents()
        {
            var agents = await _context.Agents
                .Where(a => a.Status == AgentStatus.Approved)
                .Select(a => new
                {
                    a.Id,
                    a.StoreName,
                    a.OwnerName,
                    a.Address,
                    a.Phone,
                    a.WorkingHours,
                    a.Latitude,
                    a.Longitude
                })
                .ToListAsync();

            return Ok(agents);
        }

        // ── GET /api/TransactionApi/currencies ────────────────
        // Toutes les devises actives + taux (pour conversion)
        [HttpGet("currencies")]
        public async Task<ActionResult<IEnumerable<object>>> GetCurrencies()
        {
            var currencies = await _context.Currencies
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.Code,
                    c.Name,
                    c.Symbol,
                    c.ExchangeRate,
                    c.FlagEmoji
                })
                .OrderBy(c => c.Code)
                .ToListAsync();

            return Ok(currencies);
        }
    }
}