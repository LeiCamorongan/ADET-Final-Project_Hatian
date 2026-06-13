using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Hatian.Models.Entities;

namespace Hatian.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseSplit> ExpenseSplits { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Expense>()
                .HasOne(e => e.PaidBy)
                .WithMany(p => p.ExpensesPaid)
                .HasForeignKey(e => e.PaidByParticipantId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ExpenseSplit>()
                .HasOne(es => es.Participant)
                .WithMany(p => p.ExpenseSplits)
                .HasForeignKey(es => es.ParticipantId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ExpenseSplit>()
                .HasOne(es => es.Expense)
                .WithMany(e => e.Splits)
                .HasForeignKey(es => es.ExpenseId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}