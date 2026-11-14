using System;

using LockerEtl.Models.Enums;

namespace LockerEtl.Models
{
    /// <summary>Summary of one batch run.</summary>
    public class EtlReport
    {
        public required int RowsRead { get; init; }
        public required IReadOnlyList<string> ParseErrors { get; init; }
        public required int DuplicatesRemoved { get; init; }
        public required int ParcelCount { get; init; }
        public required IReadOnlyList<RejectedParcel> Rejected { get; init; }
        public required IReadOnlyList<ParcelOutcome> Outcomes { get; init; }

        /// <summary>True when the run was stopped early (Ctrl+C). Remaining parcels were not touched.</summary>
        public bool Cancelled { get; init; }

        /// <summary>
        /// True when a person should look at the run: bad rows, rejected parcels, or any parcel
        /// that did not end as Delivered or AlreadyAllocated (no capacity, notification failed, error).
        /// </summary>
        public bool NeedsAttention =>
            ParseErrors.Count > 0
            || Rejected.Count > 0
            || Outcomes.Any(o => o.Status is not (ParcelOutcomeStatus.Delivered or ParcelOutcomeStatus.AlreadyAllocated));
    }
}