using System.ComponentModel.DataAnnotations.Schema;

namespace Hatian.Models.Entities
{
    public class Settlement
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public Guid DebtorParticipantId { get; set; }

        public Guid CreditorParticipantId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid

        public Event Event { get; set; } = null!;

        [ForeignKey(nameof(DebtorParticipantId))]
        public Participant Debtor { get; set; } = null!;

        [ForeignKey(nameof(CreditorParticipantId))]
        public Participant Creditor { get; set; } = null!;
    }
}
