using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public class Beneficiary
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } 

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } 

        // Account-to-account transfer
        [StringLength(20)]
        public string? AccountNumber { get; set; }

        // OMT-style mobile transfer
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(5)]
        public string Currency { get; set; } = "USD";

        [StringLength(60)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? Nickname { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
