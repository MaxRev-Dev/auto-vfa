using AutoVFA.Misc;
using System.IO;
using System.Linq;
using Xunit;

namespace AutoVFA_Tests
{
    public class FilenameTests : TestAbstractions
    {
        [Fact]
        public void ComputesPairs()
        {
            var result = Extensions.GetSimilarFileNames(Files.Select(x => x as string[]).SelectMany(x => x));
            Assert.True(result.Any());
            var first = result.First();
            var target = Path.GetFileName(first.Key);
            foreach (var path in first.Value)
            {
                Assert.False(string.Equals(target, path));
                Assert.True(target!.Length < path.Length);
            }
        }
    }
}