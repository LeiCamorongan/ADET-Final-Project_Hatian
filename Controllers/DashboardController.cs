using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hatian.Data;
using Hatian.Models.ViewModels;
using System.Security.Claims;

namespace Hatian.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var events = await _db.Events
                .Include(e => e.Participants)
                .Include(e => e.Expenses)
                .Where(e => e.CreatedByUserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                ActiveEvents = events.Where(e => e.Status == "Active").ToList(),
                CompletedEvents = events.Where(e => e.Status == "Completed").ToList()
            };

            return View(vm);
        }
    }
}