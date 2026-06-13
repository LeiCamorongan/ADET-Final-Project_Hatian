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
            var ev = await _db.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            return View(new GuestEnterViewModel
            {
                EventId = ev.Id
            });
        }

        [HttpPost]
        public async Task<IActionResult> EnterName(
            GuestEnterViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var existingGuest =
                await _db.Participants
                .FirstOrDefaultAsync(p =>
                    p.EventId == vm.EventId &&
                    p.Name.ToLower() ==
                    vm.GuestName.Trim().ToLower());

            Guid participantId;

            if (existingGuest != null)
            {
                participantId = existingGuest.Id;
            }
            else
            {
                var newGuest = new Participant
                {
                    Id = Guid.NewGuid(),
                    EventId = vm.EventId,
                    Name = vm.GuestName.Trim()
                };

                _db.Participants.Add(newGuest);

                await _db.SaveChangesAsync();

                participantId = newGuest.Id;
            }

            return RedirectToAction(
                "GuestDashboard",
                new
                {
                    eventId = vm.EventId,
                    participantId
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
                ShareItems = new List<GuestShareItem>()
            };

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

                    vm.ShareItems.Add(
                        new GuestShareItem
                        {
                            ExpenseDescription =
                                exp.Description,
                            PayerName =
                                exp.PaidBy.Name,
                            YourProportionalShare =
                                Math.Round(share, 2),
                            WasPaidByMe = iPaid
                        });

                    if (!iPaid)
                        vm.TotalOwed += share;
                }
            }
            ViewData["IsGuest"] = true;
            ViewData["GuestName"] = currentGuest.Name;
            return View(vm);
        }
    }
}