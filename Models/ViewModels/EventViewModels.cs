using System.ComponentModel.DataAnnotations;
using Hatian.Models.Entities;

namespace Hatian.Models.ViewModels
{
    public class FriendEntry
    {
        public string Name { get; set; } = string.Empty;

        public string Contribution { get; set; } = string.Empty;
    }

    public class CreateEventViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public string OrganizerNameAsParticipant { get; set; } = string.Empty;

        public string? OrganizerContribution { get; set; }

        public List<FriendEntry> Friends { get; set; } = new();
    }

    public class AddExpenseViewModel
    {
        public Guid EventId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public Guid PaidByParticipantId { get; set; }

        public List<Guid> SplitAmongParticipantIds { get; set; } = new();
    }

    public class DebtItem
    {
        public Guid DebtorParticipantId { get; set; }

        public string DebtorName { get; set; } = string.Empty;

        public Guid CreditorParticipantId { get; set; }

        public string CreditorName { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }

    public class EventDetailViewModel
    {
        public Event Event { get; set; } = null!;

        public List<Participant> Participants { get; set; } = new();

        public List<Expense> Expenses { get; set; } = new();

        public string CurrentUserId { get; set; } = string.Empty;

        public List<DebtItem> ComputedDebts { get; set; } = new();

        public AddExpenseViewModel NewExpense { get; set; } = new();
    }

    public class JoinEventViewModel
    {
        [Required]
        public string InviteToken { get; set; } = string.Empty;

    }

    public class GuestEnterViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        [Required]
        public string GuestName { get; set; } = string.Empty;

        public string EventTitle { get; set; } = string.Empty;

        public List<Hatian.Models.Entities.Participant> Participants { get; set; } = new();
    }

    public class GuestShareItem
    {
        public string ExpenseDescription { get; set; } = string.Empty;

        public string PayerName { get; set; } = string.Empty;

        public Guid PayerParticipantId { get; set; }

        public decimal YourProportionalShare { get; set; }

        public bool WasPaidByMe { get; set; }

        public bool IsDebtPaid { get; set; } = false;
    }

    public class GuestDebtItem
    {
        public string CreditorName { get; set; } = string.Empty;
        public Guid CreditorParticipantId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }

    public class GuestOwedItem
    {
        public string DebtorName { get; set; } = string.Empty;
        public Guid DebtorParticipantId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }

    public class GuestDashboardViewModel
    {
        public Event Event { get; set; } = null!;

        public Participant CurrentGuest { get; set; } = null!;

        public List<GuestShareItem> ShareItems { get; set; } = new();

        public List<GuestDebtItem> NettedDebts { get; set; } = new();

        public List<GuestOwedItem> OwedToMe { get; set; } = new();

        public decimal TotalOwed { get; set; }

        public decimal TotalOwedToMe { get; set; }

        public string PaidDebtKeys { get; set; } = string.Empty;
    }
}