using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public class Beneficiary
    {
        [Key]
        public int Id { get; set; }

        // Clé étrangère vers l'utilisateur (comme dans PDF 11 - ForeignKey UserId)
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        // Pour transfert account-to-account
        [StringLength(20)]
        public string? AccountNumber { get; set; }

        // Pour transfert style OMT (par téléphone)
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
