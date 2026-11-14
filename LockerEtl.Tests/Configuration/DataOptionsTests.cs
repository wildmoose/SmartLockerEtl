using System;
using LockerEtl.Configuration;

namespace LockerEtl.Tests.Configuration
{
    public class DataOptionsTests
    {
        [Fact]
        public void ResolvePath_RelativePath_IsResolvedAgainstBaseDirectory()
        {
            var baseDirectory = Path.Combine(Path.GetTempPath(), "app");
            var options = new DataOptions { Path = "Data/datasource.csv" };

            var resolved = options.ResolvePath(baseDirectory);

            Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "Data", "datasource.csv")), resolved);
        }

        [Fact]
        public void ResolvePath_AbsolutePath_IsUsedAsIs()
        {
            var absolute = Path.Combine(Path.GetTempPath(), "batch", "file.csv");
            var options = new DataOptions { Path = absolute };

            Assert.Equal(absolute, options.ResolvePath(Path.Combine(Path.GetTempPath(), "app")));
        }
    }
}