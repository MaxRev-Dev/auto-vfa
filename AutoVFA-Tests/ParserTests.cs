using System.Collections.Generic;
using AutoVFA.Parsers;
using Xunit;

namespace AutoVFA_Tests
{
    public class ParserTests : TestAbstractions
    { 
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
