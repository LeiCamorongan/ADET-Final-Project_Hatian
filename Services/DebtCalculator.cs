using Hatian.Models.Entities;
using Hatian.Models.ViewModels;

namespace Hatian.Services
{
    public static class DebtCalculator
    {
        public static List<DebtItem> ComputeDebts(Event ev)
        {
            // Start everyone at 0
            var balances = ev.Participants
                .ToDictionary(p => p.Id, p => 0m);

            foreach (var exp in ev.Expenses)
            {
                if (exp.Splits.Count == 0)
                    continue;

                decimal share = exp.Amount / exp.Splits.Count;

                // Payer is credited the full amount they fronted
                balances[exp.PaidByParticipantId] += exp.Amount;

                // Everyone in the split (including the payer, if they're in it)
                // owes their proportional share
                foreach (var split in exp.Splits)
                {
                    balances[split.ParticipantId] -= share;
                }
            }

            // Positive balance = is owed money (creditor)
            // Negative balance = owes money (debtor)
            var creditors = balances
                .Where(b => b.Value > 0.01m)
                .Select(b => (Id: b.Key, Amount: b.Value))
                .OrderByDescending(b => b.Amount)
                .ToList();

            var debtors = balances
                .Where(b => b.Value < -0.01m)
                .Select(b => (Id: b.Key, Amount: -b.Value))
                .OrderByDescending(b => b.Amount)
                .ToList();

            var names = ev.Participants.ToDictionary(p => p.Id, p => p.Name);
            var debts = new List<DebtItem>();

            int ci = 0, di = 0;

            while (ci < creditors.Count && di < debtors.Count)
            {
                var creditor = creditors[ci];
                var debtor = debtors[di];

                decimal amount = Math.Min(creditor.Amount, debtor.Amount);

                debts.Add(new DebtItem
                {
                    DebtorName = names[debtor.Id],
                    CreditorName = names[creditor.Id],
                    Amount = Math.Round(amount, 2)
                });

                creditors[ci] = (creditor.Id, creditor.Amount - amount);
                debtors[di] = (debtor.Id, debtor.Amount - amount);

                if (creditors[ci].Amount <= 0.01m) ci++;
                if (debtors[di].Amount <= 0.01m) di++;
            }

            return debts;
        }
    }
}