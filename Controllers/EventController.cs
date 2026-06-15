using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hatian.Data;
using Hatian.Models.Entities;
using Hatian.Models.ViewModels;
using Hatian.Services;

namespace Hatian.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateEventViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields to create an event.";
                return RedirectToAction("Index", "Dashboard");
            }

            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = vm.Title,
                Description = vm.Description,
                Date = vm.Date,
                CreatedByUserId = _userManager.GetUserId(User)!
            };

            var organizerParticipant = new Participant
            {
                Id = Guid.NewGuid(),
                EventId = newEvent.Id,
                Name = vm.OrganizerNameAsParticipant,
                Contribution = vm.OrganizerContribution,
                IsPaid = true,
                IsOrganizer = true
            };

            _db.Events.Add(newEvent);
            _db.Participants.Add(organizerParticipant);

            foreach (var friend in (vm.Friends ?? new List<FriendEntry>())
                .Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                _db.Participants.Add(new Participant
                {
                    Id = Guid.NewGuid(),
                    EventId = newEvent.Id,
                    Name = friend.Name,
                    Contribution = friend.Contribution,
                    IsPaid = false,
                    IsOrganizer = false
                });
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> DetailsPartial(Guid id)
        {
            var ev = await _db.Events
                .Include(e => e.Participants)
                .Include(e => e.Expenses)
                    .ThenInclude(x => x.PaidBy)
                .Include(e => e.Expenses)
                    .ThenInclude(x => x.Splits)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound();

            var vm = new EventDetailViewModel
            {
                Event = ev,
                Participants = ev.Participants.ToList(),
                Expenses = ev.Expenses.ToList(),
                CurrentUserId = _userManager.GetUserId(User)!,
                ComputedDebts = DebtCalculator.ComputeDebts(ev)
            };

            return PartialView("_EventDetailsPartial", vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddExpense(
            [Bind(Prefix = "NewExpense")] AddExpenseViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var expense = new Expense
                {
                    Id = Guid.NewGuid(),
                    EventId = vm.EventId,
                    Description = vm.Description,
                    Amount = vm.Amount,
                    PaidByParticipantId = vm.PaidByParticipantId
                };

                _db.Expenses.Add(expense);

                var splitCount = vm.SplitAmongParticipantIds.Count;
                var amountPerPerson = splitCount > 0 ? vm.Amount / splitCount : 0;

                foreach (var participantId in vm.SplitAmongParticipantIds)
                {
                    _db.ExpenseSplits.Add(new ExpenseSplit
                    {
                        Id = Guid.NewGuid(),
                        ExpenseId = expense.Id,
                        ParticipantId = participantId,
                        AmountOwed = Math.Round(amountPerPerson, 2)
                    });
                }

                // Create ExpensePayer record for the person who paid
                _db.ExpensePayers.Add(new ExpensePayer
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expense.Id,
                    ParticipantId = vm.PaidByParticipantId,
                    AmountPaid = vm.Amount
                });

                await _db.SaveChangesAsync();

                var ev = await _db.Events.FindAsync(vm.EventId);
                if (ev != null)
                {
                    ev.Status = "Active";
                    await _db.SaveChangesAsync();
                }
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteExpense(Guid expenseId, Guid eventId)
        {
            var expense = await _db.Expenses
                .Include(e => e.Splits)
                .Include(e => e.Payers)
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense != null)
            {
                _db.ExpenseSplits.RemoveRange(expense.Splits);
                _db.ExpensePayers.RemoveRange(expense.Payers);
                _db.Expenses.Remove(expense);
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EditExpense(Guid expenseId, string description, decimal amount, Guid paidByParticipantId, List<Guid> splitParticipantIds)
        {
            var expense = await _db.Expenses
                .Include(e => e.Splits)
                .Include(e => e.Payers)
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense == null) return NotFound();

            expense.Description = description;
            expense.Amount = amount;
            expense.PaidByParticipantId = paidByParticipantId;

            // Remove old splits and payers
            _db.ExpenseSplits.RemoveRange(expense.Splits);
            _db.ExpensePayers.RemoveRange(expense.Payers);

            var splitCount = splitParticipantIds.Count;
            var amountPerPerson = splitCount > 0 ? amount / splitCount : 0;

            foreach (var participantId in splitParticipantIds)
            {
                _db.ExpenseSplits.Add(new ExpenseSplit
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expenseId,
                    ParticipantId = participantId,
                    AmountOwed = Math.Round(amountPerPerson, 2)
                });
            }

            // Re-create ExpensePayer record
            _db.ExpensePayers.Add(new ExpensePayer
            {
                Id = Guid.NewGuid(),
                ExpenseId = expenseId,
                ParticipantId = paidByParticipantId,
                AmountPaid = amount
            });

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddMember(Guid eventId, string name, string contribution)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return NotFound();

            _db.Participants.Add(new Participant
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = name,
                Contribution = contribution,
                IsPaid = false,
                IsOrganizer = false
            });

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EditMember(Guid participantId, Guid eventId, string name, string contribution)
        {
            var participant = await _db.Participants
                .FirstOrDefaultAsync(p => p.Id == participantId && p.EventId == eventId);

            if (participant == null) return NotFound();

            participant.Name = name?.Trim() ?? participant.Name;
            participant.Contribution = contribution?.Trim();

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDebtPaid(Guid eventId, Guid debtorParticipantId, Guid creditorParticipantId)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return NotFound();

            var key = $"{debtorParticipantId}_{creditorParticipantId}";
            var keys = string.IsNullOrWhiteSpace(ev.PaidDebtKeys)
                ? new List<string>()
                : ev.PaidDebtKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (keys.Contains(key))
                keys.Remove(key);
            else
                keys.Add(key);

            ev.PaidDebtKeys = string.Join(",", keys);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(
            Guid participantId,
            Guid eventId)
        {
            var participant = await _db.Participants.FindAsync(participantId);

            if (participant != null)
            {
                // Toggle: if already paid, mark unpaid; otherwise mark paid
                participant.IsPaid = !participant.IsPaid;
                await _db.SaveChangesAsync();

                var ev = await _db.Events
                    .Include(e => e.Participants)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (ev != null)
                {
                    if (ev.Participants.Where(p => !p.IsOrganizer).All(p => p.IsPaid))
                    {
                        ev.Status = "Completed";
                    }
                    else
                    {
                        ev.Status = "Active";
                    }
                    await _db.SaveChangesAsync();
                }
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveMember(Guid participantId, Guid eventId)
        {
            var participant = await _db.Participants
                .Include(p => p.ExpenseSplits)
                .Include(p => p.ExpensePayers)
                .Include(p => p.ExpensesPaid)
                .FirstOrDefaultAsync(p => p.Id == participantId && p.EventId == eventId);

            if (participant == null)
                return NotFound();

            // Don't allow removing the organizer
            if (participant.IsOrganizer)
                return BadRequest("Cannot remove the organizer.");

            // Remove their expense splits
            _db.ExpenseSplits.RemoveRange(participant.ExpenseSplits);

            // Remove their expense payer records
            _db.ExpensePayers.RemoveRange(participant.ExpensePayers);

            // For expenses they paid for, reassign or delete
            // Delete expenses that this participant paid for
            foreach (var expense in participant.ExpensesPaid.ToList())
            {
                var expenseSplits = await _db.ExpenseSplits
                    .Where(s => s.ExpenseId == expense.Id)
                    .ToListAsync();
                _db.ExpenseSplits.RemoveRange(expenseSplits);

                var expensePayers = await _db.ExpensePayers
                    .Where(ep => ep.ExpenseId == expense.Id)
                    .ToListAsync();
                _db.ExpensePayers.RemoveRange(expensePayers);

                _db.Expenses.Remove(expense);
            }

            // Remove settlements involving this participant
            var settlements = await _db.Settlements
                .Where(s => s.EventId == eventId &&
                    (s.DebtorParticipantId == participantId ||
                     s.CreditorParticipantId == participantId))
                .ToListAsync();
            _db.Settlements.RemoveRange(settlements);

            _db.Participants.Remove(participant);
            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _db.Events
                .Include(e => e.Expenses)
                    .ThenInclude(e => e.Splits)
                .Include(e => e.Expenses)
                    .ThenInclude(e => e.Payers)
                .Include(e => e.Settlements)
                .FirstOrDefaultAsync(e => e.Id == id && e.CreatedByUserId == userId);

            if (ev != null)
            {
                // Remove settlements
                if (ev.Settlements != null && ev.Settlements.Any())
                {
                    _db.Settlements.RemoveRange(ev.Settlements);
                }

                foreach (var expense in ev.Expenses)
                {
                    if (expense.Splits != null && expense.Splits.Any())
                    {
                        _db.ExpenseSplits.RemoveRange(expense.Splits);
                    }
                    if (expense.Payers != null && expense.Payers.Any())
                    {
                        _db.ExpensePayers.RemoveRange(expense.Payers);
                    }
                }

                _db.Events.Remove(ev);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}