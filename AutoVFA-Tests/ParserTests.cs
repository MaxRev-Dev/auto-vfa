using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoVFA.Parsers;
using Xunit;

namespace AutoVFA_Tests
{
    public class ParserTests
    {
        private static string GetTestDataFolder(string testDataFolder)
        {
            var startupPath = AppContext.BaseDirectory;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            var projectPath = string.Join(Path.DirectorySeparatorChar.ToString(),
                pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, testDataFolder);
        }
        public static IEnumerable<object[]> Files
        {
            get {
                return Directory.GetFiles(GetTestDataFolder("data"), "*.txt").Select(x => new[] { x });
            }
        }

        [Theory]
        [MemberData(nameof(Files))]
        public void FileIsParsedProperly(string path)
        {
            var parser = new VFASummaryParser(path);
            var result = parser.ParseFile();
            Assert.NotEmpty(result);
            Assert.True(result.ContainsKey("Injection Info"));
            var dict = result["Injection Info"] as Dictionary<string, string>;
            Assert.True(dict is { });
            Assert.True(dict.ContainsKey("Method"));
            Assert.NotEmpty(dict["Method"]);
        }

        [Theory]
        [MemberData(nameof(Files))]
        public void TableParsedProperly(string path)
        {
            var parser = new VFASummaryParser(path);
            Assert.NotEmpty(parser.ParseTable("Peak Info for Channel Front"));
        }
    }
}
