using System.ComponentModel.DataAnnotations.Schema;

namespace Hatian.Models.Entities
{
    public class Expense
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid PaidByParticipantId { get; set; }
        public Event Event { get; set; } = null!;

        [ForeignKey(nameof(PaidByParticipantId))]
        public Participant PaidBy { get; set; } = null!;

        public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
    }
}