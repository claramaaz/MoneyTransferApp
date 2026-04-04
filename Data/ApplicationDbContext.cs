using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
    }
}
