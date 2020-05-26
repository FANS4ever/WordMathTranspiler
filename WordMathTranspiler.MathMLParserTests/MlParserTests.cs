using Xunit;
using WordMathTranspiler.MathMLParser;
using System;
using System.Collections.Generic;
using System.Text;
using WordMathTranspiler.MathMLParser.Nodes;
using Newtonsoft.Json;
using System.IO;
using WordMathTranspiler.MathMLParser.Nodes.Structure;

namespace WordMathTranspiler.MathMLParser.Tests
{
    public class MlParserTests
    {
        [Fact()]
        public void MlParserTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void ParseTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Theory]
        [InlineData("./TestData/AdditionTest.xml", "./ExpectedTestResults/AdditionTest.json")]
        public void ParseNewTest(string input, string expectedResult)
        {
            Node result = MlParser.Parse(input);
            using (StreamReader expectedReader = new StreamReader(expectedResult))
            {
                Node node = Node.DeserializeJson(expectedReader.ReadToEnd());
                Assert.True(result.Equals(node), "Failed test " + input);
            }
        }
    }
}