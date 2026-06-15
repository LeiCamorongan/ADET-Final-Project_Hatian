using Hatian.Models.Entities;
using Hatian.Models.ViewModels;

namespace Hatian.Services
{
    public static class DebtCalculator
    {
        /// <summary>
        /// Computes debts on a per-expense basis and groups by debtor→creditor pair.
        /// This ensures a 1-to-many relationship: one person can owe multiple different people.
        /// </summary>
        public static List<DebtItem> ComputeDebts(Event ev)
        {
            var names = ev.Participants.ToDictionary(p => p.Id, p => p.Name);

            // Key: (debtorId, creditorId) → accumulated amount
            var debtMap = new Dictionary<(Guid DebtorId, Guid CreditorId), decimal>();

            foreach (var exp in ev.Expenses)
            {
                if (exp.Splits.Count == 0)
                    continue;

                decimal share = exp.Amount / exp.Splits.Count;

                foreach (var split in exp.Splits)
                {
                    // Skip if the person in the split is the one who paid
                    if (split.ParticipantId == exp.PaidByParticipantId)
                        continue;

                    var key = (DebtorId: split.ParticipantId, CreditorId: exp.PaidByParticipantId);

                    if (debtMap.ContainsKey(key))
                        debtMap[key] += share;
                    else
                        debtMap[key] = share;
                }
            }

            var debts = new List<DebtItem>();

            foreach (var kv in debtMap)
            {
                if (kv.Value < 0.01m)
                    continue;

                debts.Add(new DebtItem
                {
                    DebtorParticipantId = kv.Key.DebtorId,
                    DebtorName = names.GetValueOrDefault(kv.Key.DebtorId, "?"),
                    CreditorParticipantId = kv.Key.CreditorId,
                    CreditorName = names.GetValueOrDefault(kv.Key.CreditorId, "?"),
                    Amount = Math.Round(kv.Value, 2)
                });
            }

            return debts.OrderByDescending(d => d.Amount).ToList();
        }
    }
}