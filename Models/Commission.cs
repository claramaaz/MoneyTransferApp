using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public class Commission
    {
        [Key]
        public int Id { get; set; }

        // e.g. "$0 – $100"
        [StringLength(50)]
        public string? Label { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxAmount { get; set; }

        // Total rate: 2.5 = 2.5%
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; }

        // Agent gets this portion
        [Column(TypeName = "decimal(5,2)")]
        public decimal AgentShare { get; set; }

        // Platform keeps this portion
        [Column(TypeName = "decimal(5,2)")]
        public decimal PlatformShare { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
