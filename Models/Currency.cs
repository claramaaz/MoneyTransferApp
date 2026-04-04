using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public class Currency
    {
        [Key]
        [StringLength(5)]
        public string Code { get; set; }  // "USD", "EUR"

        [Required]
        [StringLength(50)]
        public string Name { get; set; }   // "US Dollar"

        [StringLength(10)]
        public string Symbol { get; set; }  // "$", "€"

        // Rate vs USD: EUR = 0.92, LBP = 89500
        [Required]
        [Column(TypeName = "decimal(18,6)")]
        public decimal ExchangeRate { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        [StringLength(10)]
        public string? FlagEmoji { get; set; }             // "🇺🇸"

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
