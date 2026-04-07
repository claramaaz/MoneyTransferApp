// IMPORTANT: on écrit le using complet pour éviter le conflit
// avec System.Transactions.Transaction
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Data;

// On crée un alias pour notre modèle Transaction
using TxModel = MoneyTransferApp.Models.Transaction;

namespace MoneyTransferApp.Repositories
{
    public class TransactionRepository : IRepository<TxModel>
    {
        private readonly ApplicationDbContext _db;

        // Dependency Injection — PDF 11 slide 16
        public TransactionRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TxModel>> GetAllAsync()
        {
            return await _db.Transactions
                .Include(t => t.Sender)
                .Include(t => t.Recipient)
                .ToListAsync();
        }

        public async Task<TxModel?> GetByIdAsync(int id)
        {
            var transaction = await _db.Transactions
                .Include(t => t.Sender)
                .Include(t => t.Recipient)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                throw new KeyNotFoundException($"Transaction #{id} not found");

            return transaction;
        }

        public async Task AddAsync(TxModel entity)
        {
            // Génère le numéro de série automatiquement
            entity.SerialNumber = $"TXN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            await _db.Transactions.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(TxModel entity)
        {
            _db.Transactions.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var transaction = await _db.Transactions.FindAsync(id);
            if (transaction == null)
                throw new KeyNotFoundException($"Transaction #{id} not found");

            _db.Transactions.Remove(transaction);
            await _db.SaveChangesAsync();
        }

        // Méthode supplémentaire: transactions d'un utilisateur
        public async Task<IEnumerable<TxModel>> GetByUserIdAsync(string userId)
        {
            return await _db.Transactions
                .Where(t => t.SenderId == userId || t.RecipientId == userId)
                .Include(t => t.Sender)
                .Include(t => t.Recipient)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}