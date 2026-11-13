using System;
using LockerEtl.Models;

namespace LockerEtl.Processing
{
    /// <summary>
    /// Turns the raw event log into one current state per parcel.
    /// Rule: for each field, the latest non-empty value wins. An empty field means
    /// "no change", not "clear the value".
    /// </summary>
    public class ParcelEventConsolidator
    {
        public ConsolidationResult Consolidate(IEnumerable<ParcelEvent> events)
        {
            var all = events.ToList();

            // 1. Drop exact duplicates (content-based equality, see ParcelEvent.Equals).
            var distinct = all.Distinct().ToList();

            // 2 + 3 + 4. Group per parcel, order by time (file order is not reliable), merge.
            var parcels = distinct
                .GroupBy(e => e.ParcelId, StringComparer.Ordinal)
                .Select(g => Merge(g.OrderBy(e => e.Timestamp).ToList()))
                .OrderBy(p => p.ParcelId, StringComparer.Ordinal)
                .ToList();

            return new ConsolidationResult
            {
                Parcels = parcels,
                DuplicatesRemoved = all.Count - distinct.Count
            };
        }

        private static ParcelState Merge(IReadOnlyList<ParcelEvent> ordered)
        {
            var latest = ordered[^1];

            return new ParcelState
            {
                ParcelId = latest.ParcelId,
                RecipientId = latest.RecipientId,
                RecipientName = LatestNonNull(ordered, e => e.RecipientName),
                RecipientPhone = LatestNonNull(ordered, e => e.RecipientPhone),
                RecipientEmail = LatestNonNull(ordered, e => e.RecipientEmail),
                RecipientPostalCode = LatestNonNull(ordered, e => e.RecipientPostalCode),
                RetailerId = LatestNonNull(ordered, e => e.RetailerId),
                TargetLockerId = LatestNonNull(ordered, e => e.TargetLockerId),
                Size = ordered.LastOrDefault(e => e.Size.HasValue)?.Size,
                LastUpdated = latest.Timestamp,
                EventCount = ordered.Count
            };
        }

        private static string? LatestNonNull(IReadOnlyList<ParcelEvent> ordered, Func<ParcelEvent, string?> selector) =>
            ordered.Select(selector).LastOrDefault(v => v is not null);
    }
}