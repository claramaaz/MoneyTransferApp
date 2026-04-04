using System.ComponentModel.DataAnnotations;

namespace MoneyTransferApp.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Message { get; set; }

        // "transfer", "topup", "agent", "system"
        [StringLength(20)]
        public string Type { get; set; } = "system";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
