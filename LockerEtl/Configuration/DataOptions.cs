using System;

namespace LockerEtl.Configuration
{
    /// <summary>Binds the "data" section of appsettings.json.</summary>
    public class DataOptions
    {
        public const string SectionName = "data";
        public required string Path { get; init; }

        /// <summary>
        /// Absolute paths are used as they are. Relative paths are resolved against
        /// <paramref name="baseDirectory"/> (the application folder), never the current directory.
        /// </summary>
        public string ResolvePath(string baseDirectory)
        {
            return System.IO.Path.IsPathRooted(Path)
                ? Path
                : System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, Path));
        }
    }
}