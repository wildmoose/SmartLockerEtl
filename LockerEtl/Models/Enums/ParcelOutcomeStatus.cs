using System;

namespace LockerEtl.Models.Enums
{
    public enum ParcelOutcomeStatus
    {
        Delivered,
        AlreadyAllocated,
        Rejected,
        NoCapacity,
        NotificationFailed,
        Error
    }
}
