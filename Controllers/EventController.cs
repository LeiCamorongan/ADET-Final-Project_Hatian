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
                return View(vm);

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

            foreach (var friend in vm.Friends
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

                foreach (var participantId in vm.SplitAmongParticipantIds)
                {
                    _db.ExpenseSplits.Add(new ExpenseSplit
                    {
                        Id = Guid.NewGuid(),
                        ExpenseId = expense.Id,
                        ParticipantId = participantId
                    });
                }

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
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense != null)
            {
                _db.ExpenseSplits.RemoveRange(expense.Splits);
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
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense == null) return NotFound();

            expense.Description = description;
            expense.Amount = amount;
            expense.PaidByParticipantId = paidByParticipantId;

            _db.ExpenseSplits.RemoveRange(expense.Splits);
            foreach (var participantId in splitParticipantIds)
            {
                _db.ExpenseSplits.Add(new ExpenseSplit
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expenseId,
                    ParticipantId = participantId
                });
            }

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
        public async Task<IActionResult> MarkAsPaid(
            Guid participantId,
            Guid eventId)
        {
            var participant = await _db.Participants.FindAsync(participantId);

            if (participant != null)
            {
                participant.IsPaid = true;
                await _db.SaveChangesAsync();

                var ev = await _db.Events
                    .Include(e => e.Participants)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (ev != null && ev.Participants.All(p => p.IsPaid))
                {
                    ev.Status = "Completed";
                    await _db.SaveChangesAsync();
                }
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _db.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.CreatedByUserId == userId);

            if (ev != null)
            {
                _db.Events.Remove(ev);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}