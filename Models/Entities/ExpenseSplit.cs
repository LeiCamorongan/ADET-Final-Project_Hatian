namespace Hatian.Models.Entities
{
    public class ExpenseSplit
    {
        public Guid Id { get; set; }

        public Guid ExpenseId { get; set; }

        public Guid ParticipantId { get; set; }

        public Expense Expense { get; set; } = null!;

        public Participant Participant { get; set; } = null!;
    }
}