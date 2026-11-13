using System;

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
    }
}