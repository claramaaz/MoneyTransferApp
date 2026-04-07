
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    // On hérite de IdentityUser exactement comme montré dans le PDF 10
    // IdentityUser nous donne déjà: Id, Email, UserName, PasswordHash, PhoneNumber
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Généré automatiquement à l'inscription: "ACC-2026-00847"
        [StringLength(20)]
        public string AccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        // Devise préférée: "USD", "EUR", "LBP"
        [StringLength(5)]
        public string PreferredCurrency { get; set; } = "USD";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties (PDF 11 - Repository Pattern)
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
        public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}