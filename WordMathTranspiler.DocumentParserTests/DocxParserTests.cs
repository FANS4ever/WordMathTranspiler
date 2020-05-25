using Xunit;
using WordMathTranspiler.DocumentParser;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IO;
using DocumentFormat.OpenXml.EMMA;

namespace WordMathTranspiler.DocumentParser.Tests
{
    public class DocxParserTests
    {
        private const string testAppSettings = "./testappsettings.json";
        private const string testDoc = "./testDocument.docx";
        private const string testTag = "testTag";
        private const string testDir = "./testDir";
        private const string defaultOMML = "./XSLT/OMML2MML.xsl"; //default path
        
        [Fact()]
        public void DocxParserTest()
        {
            var parser = new DocxParser(testAppSettings);
            Assert.True(parser.rootTagName == testTag, "This test needs an implementation");
            Assert.True(parser.omml2mmlPath == testDir, "This test needs an implementation");
        }

        [Fact()]
        public void DocxParserTest1()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(testAppSettings);
            var configuration = builder.Build();
            var parser = new DocxParser(configuration);
            Assert.True(parser.rootTagName == testTag, "This test needs an implementation");
            Assert.True(parser.omml2mmlPath == testDir, "This test needs an implementation");
        }

        [Fact()]
        public void DocxParserTest2()
        {
            var parser = new DocxParser(testTag, testDir);
            Assert.True(parser.rootTagName == testTag, "This test needs an implementation");
            Assert.True(parser.omml2mmlPath == testDir, "This test needs an implementation");
        }

        [Fact()]
        public void LoadConfigurationFromFileTest()
        { 
            var parser = new DocxParser(testAppSettings);
            parser.LoadConfigurationFromFile(testAppSettings);
            Assert.True(parser.rootTagName == testTag, "This test needs an implementation");
            Assert.True(parser.omml2mmlPath == testDir, "This test needs an implementation");
        }

        [Fact()]
        public void LoadConfigurationTest()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(testAppSettings);
            var configuration = builder.Build();
            var parser = new DocxParser(testAppSettings);
            parser.LoadConfiguration(configuration);
            Assert.True(parser.rootTagName == testTag, "This test needs an implementation");
            Assert.True(parser.omml2mmlPath == testDir, "This test needs an implementation");
        }

        [Fact()]
        public void ParseDocumentToFileTest()
        {
            var parser = new DocxParser(testTag, defaultOMML);
            var path = Path.Combine(testDir, Path.GetFileName(testDoc));

            try
            {
                parser.ParseDocumentToFile(testDoc, path);
                Assert.True(File.Exists(path), "Test if file exists.");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact()]
        public void ParseDocumentToFileTest1()
        {
            //DocxParser.ParseDocumentToFile();
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void ParseDocumentTest()
        {
            var parser = new DocxParser(testTag, testDir);
            //parser.ParseDocument();
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void ParseDocumentTest1()
        {
            //DocxParser.ParseDocument();
            Assert.True(false, "This test needs an implementation");
        }
    }
}