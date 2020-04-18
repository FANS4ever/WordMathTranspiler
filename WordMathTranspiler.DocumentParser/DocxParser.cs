using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Xsl;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Configuration;

namespace WordMathTranspiler.DocumentParser
{
    class DocxParser
    {
        private const string _officeMathTag = "oMath";
        private IConfigurationRoot _config;
        private string omml2mmlPath;
        private string rootTagName;

        public DocxParser(string configPath)
        {
            LoadConfigurationFromFile(configPath);
        }
        public DocxParser(string rootTagName, string omml2mmlPath)
        {
            this.omml2mmlPath = omml2mmlPath;
            this.rootTagName = rootTagName;
        }
        public DocxParser(IConfigurationRoot config)
        {
            LoadConfiguration(config);
        }
        public void LoadConfigurationFromFile(string configPath)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configPath);
            var configuration = builder.Build();
            LoadConfiguration(configuration);
        }
        public void LoadConfiguration(IConfigurationRoot config)
        {
            this._config = config;
            var omml2mmlSetting = config.GetSection("OMML2MML");
            if (omml2mmlSetting.Exists())
            {
                this.omml2mmlPath = omml2mmlSetting.Value;
            }
            else 
            {
                omml2mmlPath = @"./XSLT/OMML2MML.XSL";
            }

            var rootTagNameSetting = config.GetSection("rootTagName");
            if (rootTagNameSetting.Exists())
            {
                rootTagName = rootTagNameSetting.Value;
            }
            else
            {
                rootTagName = "data";
            }
        }
        public void ParseDocumentToFile(string inputFile, string outputFile)
        {
            ParseDocumentToFile(inputFile, outputFile, this.rootTagName, this.omml2mmlPath);
        }
        public static void ParseDocumentToFile(string inputFile, string outputFile, string rootNodeName = "data", string omml2mmlLocation = "./XSLT/OMML2MML.XSL")
        {
            using (StreamWriter outputToFile = new StreamWriter(outputFile))
            {
                outputToFile.WriteLine(ParseDocument(inputFile, rootNodeName, omml2mmlLocation));
            }
        }
        public string ParseDocument(string inputFile)
        {
            return ParseDocument(inputFile, this.rootTagName, this.omml2mmlPath);
        }
        public static string ParseDocument(string inputFile, string rootNodeName = "data", string omml2mmlLocation = "./XSLT/OMML2MML.XSL")
        {
            using (var doc = WordprocessingDocument.Open(inputFile, false))
            {
                var mathFormulas = doc.MainDocumentPart.Document.Body.Descendants().Where(e => e.LocalName == _officeMathTag);

                if (mathFormulas.Any())
                {
                    XslCompiledTransform xslTransform = new XslCompiledTransform();
                    xslTransform.Load(omml2mmlLocation);

                    XmlWriterSettings settings = new XmlWriterSettings
                    {
                        Indent = true,
                        OmitXmlDeclaration = true,
                        NewLineOnAttributes = false
                    };

                    string data = '<' + rootNodeName + '>';
                    foreach (OfficeMath item in mathFormulas)
                    {
                        using (StringWriter transformedOutput = new StringWriter())
                        using (XmlWriter xmlOutput = XmlWriter.Create(transformedOutput, settings))
                        using (XmlReader xmlInput = XmlReader.Create(new StringReader(item.OuterXml)))
                        {
                            xslTransform.Transform(xmlInput, xmlOutput);
                            data += Environment.NewLine + transformedOutput.ToString();
                        }
                    }
                    data = data.Replace(Environment.NewLine, Environment.NewLine + new String(' ', 2)); // Add indentation
                    return data + Environment.NewLine + "</" + rootNodeName + '>';
                }
            }
            return "";
        }
    }
}
