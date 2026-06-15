using System.ComponentModel.DataAnnotations.Schema;

namespace Hatian.Models.Entities
{
    public class ExpensePayer
    {
        public Guid Id { get; set; }

        public Guid ExpenseId { get; set; }

        public Guid ParticipantId { get; set; }

        public decimal AmountPaid { get; set; }

        public Expense Expense { get; set; } = null!;

        [ForeignKey(nameof(ParticipantId))]
        public Participant Participant { get; set; } = null!;
    }
}
