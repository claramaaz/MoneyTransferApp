using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTransferApp.Models
{
    public enum AgentStatus { Pending, Approved, Rejected, Suspended }

    public class Agent
    {
        [Key]
        public int Id { get; set; }

        // L'utilisateur propriétaire de ce point de vente
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        // Pour la carte Leaflet.js (PDF project brief - Agent Map)
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // JSON: {"Mon-Fri":"09:00-21:00","Sat":"10:00-18:00"}
        [StringLength(500)]
        public string? WorkingHours { get; set; }

        public AgentStatus Status { get; set; } = AgentStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCommissionEarned { get; set; } = 0;

        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}