using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MoneyTransferApp.Models
{
    public enum TransactionStatus { Pending, Completed, Failed, Cancelled }
    public enum TransactionType { Transfer, Mobile, TopUp, CashIn, CashOut }

    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        // Numéro unique de suivi: "TXN-20260404-A3F9B2"
        [Required]
        [StringLength(30)]
        public string SerialNumber { get; set; } = string.Empty;

        // Expéditeur (nullable pour les Top-Up Stripe)
        public string? SenderId { get; set; }

        
        [ForeignKey("SenderId")]
        [InverseProperty("SentTransactions")]
        public User? Sender { get; set; }

        // Destinataire (nullable pour les transferts par mobile)
        public string? RecipientId { get; set; }

        [ForeignKey("RecipientId")]
        [InverseProperty("ReceivedTransactions")]
        public User? Recipient { get; set; }

        // Pour les transferts style OMT (numéro de téléphone)
        [StringLength(20)]
        public string? RecipientPhone { get; set; }

        [StringLength(100)]
        public string? RecipientName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(5)]
        public string FromCurrency { get; set; } = "USD";

        [Required]
        [StringLength(5)]
        public string ToCurrency { get; set; } = "USD";

        // Montant après conversion automatique
        [Column(TypeName = "decimal(18,2)")]
        public decimal ConvertedAmount { get; set; }

        // Taux de change utilisé au moment du transfert
        [Column(TypeName = "decimal(18,6)")]
        public decimal ExchangeRateUsed { get; set; } = 1;

        // Commission appliquée
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; } = 0;

        [StringLength(200)]
        public string? Note { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public TransactionType Type { get; set; } = TransactionType.Transfer;

        // Agent qui a traité (pour CashIn/CashOut)
        public int? AgentId { get; set; }

        [ForeignKey("AgentId")]
        public Agent? Agent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}