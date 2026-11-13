using System;
using LockerEtl.Models;

namespace LockerEtl.Processing
{
    /// <summary>
    /// Checks that a consolidated parcel has everything needed for allocation and notification.
    /// Missing data is rejected rather than guessed, so a wrong guess never puts a parcel
    /// in the wrong city or the wrong compartment size.
    /// </summary>
    public class ParcelValidator
    {
        public ValidationResult Validate(IEnumerable<ParcelState> parcels)
        {
            var valid = new List<ParcelState>();
            var rejected = new List<RejectedParcel>();

            foreach (var parcel in parcels)
            {
                var errors = GetErrors(parcel).ToList();
                if (errors.Count == 0)
                {
                    valid.Add(parcel);
                }
                else
                {
                    rejected.Add(new RejectedParcel { ParcelId = parcel.ParcelId, Reason = string.Join(" ", errors) });
                }
            }

            return new ValidationResult { Valid = valid, Rejected = rejected };
        }

        private static IEnumerable<string> GetErrors(ParcelState parcel)
        {
            if (parcel.Size is null)
                yield return "Missing parcel size.";

            if (parcel.TargetLockerId is null)
                yield return "Missing target locker.";

            if (parcel.RecipientPhone is null && parcel.RecipientEmail is null)
                yield return "Recipient has neither phone nor email.";
        }
    }
}