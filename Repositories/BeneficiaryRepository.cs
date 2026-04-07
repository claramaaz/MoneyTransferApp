using MoneyTransferApp.Data;
using MoneyTransferApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MoneyTransferApp.Repositories
{
    public class BeneficiaryRepository : IRepository<Beneficiary>
    {
        private readonly ApplicationDbContext _db;

        public BeneficiaryRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Beneficiary>> GetAllAsync()
        {
            return await _db.Beneficiaries.Include(b => b.User).ToListAsync();
        }

        public async Task<Beneficiary?> GetByIdAsync(int id)
        {
            var beneficiary = await _db.Beneficiaries.FindAsync(id);
            if (beneficiary == null)
                throw new KeyNotFoundException($"Beneficiary #{id} not found");
            return beneficiary;
        }

        public async Task AddAsync(Beneficiary entity)
        {
            await _db.Beneficiaries.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Beneficiary entity)
        {
            _db.Beneficiaries.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var beneficiary = await _db.Beneficiaries.FindAsync(id);
            if (beneficiary == null)
                throw new KeyNotFoundException($"Beneficiary #{id} not found");
            _db.Beneficiaries.Remove(beneficiary);
            await _db.SaveChangesAsync();
        }

        // Méthode spécifique: bénéficiaires d'un utilisateur
        public async Task<IEnumerable<Beneficiary>> GetByUserIdAsync(string userId)
        {
            return await _db.Beneficiaries
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }
    }
}