using MoneyTransferApp.Data;
using MoneyTransferApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MoneyTransferApp.Repositories
{
    public class AgentRepository : IRepository<Agent>
    {
        private readonly ApplicationDbContext _db;

        public AgentRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Agent>> GetAllAsync()
        {
            return await _db.Agents.Include(a => a.User).ToListAsync();
        }

        public async Task<Agent?> GetByIdAsync(int id)
        {
            var agent = await _db.Agents.FindAsync(id);
            if (agent == null)
                throw new KeyNotFoundException($"Agent #{id} not found");
            return agent;
        }

        public async Task AddAsync(Agent entity)
        {
            await _db.Agents.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Agent entity)
        {
            _db.Agents.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var agent = await _db.Agents.FindAsync(id);
            if (agent == null)
                throw new KeyNotFoundException($"Agent #{id} not found");
            _db.Agents.Remove(agent);
            await _db.SaveChangesAsync();
        }

        // Agents approuvés seulement (pour la carte)
        public async Task<IEnumerable<Agent>> GetApprovedAgentsAsync()
        {
            return await _db.Agents
                .Where(a => a.Status == AgentStatus.Approved)
                .ToListAsync();
        }
    }
}