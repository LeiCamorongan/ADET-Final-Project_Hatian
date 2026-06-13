using System.ComponentModel.DataAnnotations.Schema;

namespace Hatian.Models.Entities
{
    public class Participant
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsPaid { get; set; } = false;

        public string? Contribution { get; set; }

        public bool IsOrganizer { get; set; } = false;

        public Event Event { get; set; } = null!;

        [InverseProperty("PaidBy")]
        public ICollection<Expense> ExpensesPaid { get; set; }
            = new List<Expense>();

        public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
    }
}