using System;

namespace LockerEtl.Models.Enums
{
    public enum ParcelOutcomeStatus
    {
        Delivered,
        AlreadyAllocated,
        NoCapacity,
        NotificationFailed,
        Error
    }
}