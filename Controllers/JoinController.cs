using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hatian.Data;
using Hatian.Models.Entities;
using Hatian.Models.ViewModels;

namespace Hatian.Controllers
{
    public class JoinController : Controller
    {
        private readonly ApplicationDbContext _db;

        public JoinController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new JoinEventViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(JoinEventViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var ev = await _db.Events
                .FirstOrDefaultAsync(e =>
                    e.InviteToken ==
                    vm.InviteToken.Trim().ToUpper());

            if (ev == null)
            {
                ModelState.AddModelError("",
                    "Event not found.");

                return View(vm);
            }

            return RedirectToAction(
                "EnterName",
                new { eventId = ev.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EnterName(Guid eventId)
        {
            var ev = await _db.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return NotFound();

            return View(new GuestEnterViewModel
            {
                EventId = ev.Id,
                EventTitle = ev.Title,
                Participants = ev.Participants.ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> EnterName(
            GuestEnterViewModel vm)
        {
            // Re-load participants for the view in case of validation failure
            var ev = await _db.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == vm.EventId);

            if (ev == null)
                return NotFound();

            vm.Participants = ev.Participants.ToList();
            vm.EventTitle = ev.Title;

            if (!ModelState.IsValid)
                return View(vm);

            // Only allow existing participant names — no new creation
            var existingGuest =
                await _db.Participants
                .FirstOrDefaultAsync(p =>
                    p.EventId == vm.EventId &&
                    p.Name.ToLower() ==
                    vm.GuestName.Trim().ToLower());

            if (existingGuest == null)
            {
                ModelState.AddModelError("GuestName",
                    "That name doesn't exist in this event. Please select your name from the list.");
                return View(vm);
            }

            return RedirectToAction(
                "GuestDashboard",
                new
                {
                    eventId = vm.EventId,
                    participantId = existingGuest.Id
                });
        }

        [HttpGet]
        public async Task<IActionResult> GuestDashboard(
            Guid eventId,
            Guid participantId)
        {
            var ev = await _db.Events
                .Include(e => e.Participants)
                .Include(e => e.Expenses)
                    .ThenInclude(ex => ex.PaidBy)
                .Include(e => e.Expenses)
                    .ThenInclude(ex => ex.Splits)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            var currentGuest =
                await _db.Participants.FindAsync(participantId);

            if (ev == null || currentGuest == null)
                return NotFound();

            var vm = new GuestDashboardViewModel
            {
                Event = ev,
                CurrentGuest = currentGuest,
                TotalOwed = 0m,
                ShareItems = new List<GuestShareItem>(),
                PaidDebtKeys = ev.PaidDebtKeys ?? string.Empty
            };

            // Parse paid debt keys
            var paidKeys = new HashSet<string>(
                string.IsNullOrWhiteSpace(ev.PaidDebtKeys)
                    ? Array.Empty<string>()
                    : ev.PaidDebtKeys.Split(',', StringSplitOptions.RemoveEmptyEntries));

            // Use DebtCalculator to get netted debts for accurate total
            var nettedDebts = Services.DebtCalculator.ComputeDebts(ev);

            // Calculate net total owed: sum of debts where I'm the debtor, minus debts where I'm the creditor
            foreach (var debt in nettedDebts)
            {
                var debtKey = $"{debt.DebtorParticipantId}_{debt.CreditorParticipantId}";
                var isDebtPaid = paidKeys.Contains(debtKey);

                if (debt.DebtorParticipantId == currentGuest.Id)
                {
                    vm.NettedDebts.Add(new GuestDebtItem
                    {
                        CreditorName = debt.CreditorName,
                        CreditorParticipantId = debt.CreditorParticipantId,
                        Amount = debt.Amount,
                        IsPaid = isDebtPaid
                    });

                    if (!isDebtPaid)
                    {
                        vm.TotalOwed += debt.Amount;
                    }
                }
                else if (debt.CreditorParticipantId == currentGuest.Id)
                {
                    vm.OwedToMe.Add(new GuestOwedItem
                    {
                        DebtorName = debt.DebtorName,
                        DebtorParticipantId = debt.DebtorParticipantId,
                        Amount = debt.Amount,
                        IsPaid = isDebtPaid
                    });

                    if (!isDebtPaid)
                    {
                        vm.TotalOwedToMe += debt.Amount;
                    }
                }
            }

            // Build per-expense share items for breakdown display
            foreach (var exp in ev.Expenses)
            {
                if (exp.Splits.Any(
                    s => s.ParticipantId == currentGuest.Id))
                {
                    decimal share =
                        exp.Amount / exp.Splits.Count;

                    bool iPaid =
                        exp.PaidByParticipantId ==
                        currentGuest.Id;

                    // Check if this specific debt (me → payer) has been marked as paid
                    var debtKey = $"{currentGuest.Id}_{exp.PaidByParticipantId}";
                    var isDebtPaid = !iPaid && paidKeys.Contains(debtKey);

                    vm.ShareItems.Add(
                        new GuestShareItem
                        {
                            ExpenseDescription =
                                exp.Description,
                            PayerName =
                                exp.PaidBy.Name,
                            PayerParticipantId =
                                exp.PaidByParticipantId,
                            YourProportionalShare =
                                Math.Round(share, 2),
                            WasPaidByMe = iPaid,
                            IsDebtPaid = isDebtPaid
                        });
                }
            }
            ViewData["IsGuest"] = true;
            ViewData["GuestName"] = currentGuest.Name;
            return View(vm);
        }
    }
}