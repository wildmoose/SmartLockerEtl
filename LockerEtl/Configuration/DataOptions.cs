using System;

namespace LockerEtl.Configuration
{
    /// <summary>Binds the "data" section of appsettings.json.</summary>
    public class DataOptions
    {
        public const string SectionName = "data";
        public required string Path { get; init; }
    }
}
