using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
        public class Currency
        {
            // La clé primaire EST le code: "USD", "EUR", "LBP"
            [Key]
            [StringLength(5)]
            public string Code { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            public string Name { get; set; } = string.Empty;

            [StringLength(10)]
            public string Symbol { get; set; } = string.Empty;

            // Taux vs USD: EUR=0.92, LBP=89500
            [Required]
            [Column(TypeName = "decimal(18,6)")]
            public decimal ExchangeRate { get; set; } = 1;

            public bool IsActive { get; set; } = true;

            [StringLength(10)]
            public string? FlagEmoji { get; set; }

            public DateTime LastUpdated { get; set; } = DateTime.Now;
        }
    
}