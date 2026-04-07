using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public class Commission
    {
        [Key]
        public int Id { get; set; }

        // Ex: "$0 – $100"
        [StringLength(50)]
        public string? Label { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxAmount { get; set; }

        // Taux total: 2.5 signifie 2.5%
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; }

        // Part de l'agent
        [Column(TypeName = "decimal(5,2)")]
        public decimal AgentShare { get; set; }

        // Part de la plateforme
        [Column(TypeName = "decimal(5,2)")]
        public decimal PlatformShare { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}