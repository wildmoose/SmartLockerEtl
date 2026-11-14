using LockerEtl.Models.Enums;

namespace LockerEtl.Models
{
    /// <summary>
    /// One row from the core system batch. A parcel can appear in many events;
    /// fields that were empty in the CSV are null.
    /// </summary>
    /// <remarks>
    /// Equality is based on content, not on <see cref="EventId"/>. The core system can emit
    /// the same event twice with different event ids (e.g. EVT-1047 and EVT-1048), and those
    /// must be treated as duplicates.
    /// </remarks>
    public class ParcelEvent : IEquatable<ParcelEvent>
    {
        public required string EventId { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
        public required string ParcelId { get; init; }
        public required string RecipientId { get; init; }
        public string? RecipientName { get; init; }
        public string? RecipientPhone { get; init; }
        public string? RecipientEmail { get; init; }
        public string? RecipientPostalCode { get; init; }
        public string? RetailerId { get; init; }
        public string? TargetLockerId { get; init; }
        public ParcelSize? Size { get; init; }

        #region Equality

        public bool Equals(ParcelEvent? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            // The class is not sealed, so a subclass instance must never equal a plain ParcelEvent.
            if (GetType() != other.GetType()) return false;

            return Timestamp == other.Timestamp
                && string.Equals(ParcelId, other.ParcelId, StringComparison.Ordinal)
                && string.Equals(RecipientId, other.RecipientId, StringComparison.Ordinal)
                && string.Equals(RecipientName, other.RecipientName, StringComparison.Ordinal)
                && string.Equals(RecipientPhone, other.RecipientPhone, StringComparison.Ordinal)
                && string.Equals(RecipientEmail, other.RecipientEmail, StringComparison.OrdinalIgnoreCase)
                && string.Equals(RecipientPostalCode, other.RecipientPostalCode, StringComparison.Ordinal)
                && string.Equals(RetailerId, other.RetailerId, StringComparison.Ordinal)
                && string.Equals(TargetLockerId, other.TargetLockerId, StringComparison.Ordinal)
                && Size == other.Size;
        }

        public override bool Equals(object? obj) => Equals(obj as ParcelEvent);

        public override int GetHashCode()
        {
            // HashCode.Combine takes at most 8 values, so use the builder form.
            var hash = new HashCode();
            hash.Add(Timestamp);
            hash.Add(ParcelId, StringComparer.Ordinal);
            hash.Add(RecipientId, StringComparer.Ordinal);
            hash.Add(RecipientName, StringComparer.Ordinal);
            hash.Add(RecipientPhone, StringComparer.Ordinal);
            hash.Add(RecipientEmail, StringComparer.OrdinalIgnoreCase);
            hash.Add(RecipientPostalCode, StringComparer.Ordinal);
            hash.Add(RetailerId, StringComparer.Ordinal);
            hash.Add(TargetLockerId, StringComparer.Ordinal);
            hash.Add(Size);
            return hash.ToHashCode();
        }

        public static bool operator ==(ParcelEvent? left, ParcelEvent? right) => Equals(left, right);
        public static bool operator !=(ParcelEvent? left, ParcelEvent? right) => !Equals(left, right);

        #endregion
    }
}