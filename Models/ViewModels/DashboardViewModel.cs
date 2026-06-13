using Hatian.Models.Entities;

namespace Hatian.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<Event> ActiveEvents { get; set; } = new();
        public List<Event> CompletedEvents { get; set; } = new();
    }
}