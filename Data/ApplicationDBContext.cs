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
        public DbSet<ExpensePayer> ExpensePayers { get; set; }
        public DbSet<Settlement> Settlements { get; set; }

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

            builder.Entity<ExpenseSplit>()
                .Property(es => es.AmountOwed)
                .HasPrecision(18, 2);

            // ExpensePayer configuration
            builder.Entity<ExpensePayer>()
                .HasOne(ep => ep.Expense)
                .WithMany(e => e.Payers)
                .HasForeignKey(ep => ep.ExpenseId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ExpensePayer>()
                .HasOne(ep => ep.Participant)
                .WithMany(p => p.ExpensePayers)
                .HasForeignKey(ep => ep.ParticipantId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ExpensePayer>()
                .Property(ep => ep.AmountPaid)
                .HasPrecision(18, 2);

            // Settlement configuration
            builder.Entity<Settlement>()
                .HasOne(s => s.Event)
                .WithMany(e => e.Settlements)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Settlement>()
                .HasOne(s => s.Debtor)
                .WithMany(p => p.Debts)
                .HasForeignKey(s => s.DebtorParticipantId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Settlement>()
                .HasOne(s => s.Creditor)
                .WithMany(p => p.Credits)
                .HasForeignKey(s => s.CreditorParticipantId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Settlement>()
                .Property(s => s.Amount)
                .HasPrecision(18, 2);
        }
    }
}