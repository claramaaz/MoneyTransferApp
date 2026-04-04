
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } 

        [Required]
        [StringLength(150)]
        public string Email { get; set; } 

        // We store the password as a hash (use BCrypt or SHA256)
        [Required]
        public string PasswordHash { get; set; } 

        // Auto-generated on register: "ACC-2026-00847"
        [StringLength(20)]
        public string AccountNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } 

        // "USD", "EUR", "LBP", "AED", etc.
        [StringLength(5)]
        public string PreferredCurrency { get; set; } = "USD";

        // "User", "Agent", "Admin"
        [StringLength(20)]
        public string Role { get; set; } = "User";

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
        public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
