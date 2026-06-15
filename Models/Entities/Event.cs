using System.ComponentModel.DataAnnotations;

namespace Hatian.Models.Entities
{
    public class Event
    {
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime Date { get; set; }

        public string CreatedByUserId { get; set; } = string.Empty;

        public string Status { get; set; } = "Active"; // Active or Completed

        public string InviteToken { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

        public string PaidDebtKeys { get; set; } = string.Empty;

        public ICollection<Participant> Participants { get; set; } = new List<Participant>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    }
}