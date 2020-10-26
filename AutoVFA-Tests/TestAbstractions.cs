using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoVFA_Tests
{
    public class TestAbstractions
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
    }
}