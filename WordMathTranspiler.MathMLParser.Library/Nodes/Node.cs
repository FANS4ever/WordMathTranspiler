using System;
using WordMathTranspiler.MathMLParser.Nodes.Structure;
using Newtonsoft.Json;

namespace WordMathTranspiler.MathMLParser.Nodes
{
    /// <summary>
    /// Base node class
    /// </summary>
    public abstract class Node
    {
        #region Abstract methods   
        // Maybe move to semantic analyzer?
        public abstract bool IsFloatPointOperation();
        public abstract string TextPrint();
        public abstract string TreePrint();
        public abstract string DotPrint(ref int id);
        #endregion

        #region Helpers
        /// <summary>
        /// Adds indentation to multiline strings.
        /// </summary>
        /// <param name="src">Input string</param>
        /// <param name="count">Indentation level (spaces). Default: 5</param>
        /// <param name="drawSeperator">Should the method draw the '│' vertical seperator. Default: false</param>
        /// <returns></returns>
        public static string BuildDotGraph(Node root)
        {
            int idCounter = 0;
            return $"digraph graphname{{\n{ root.DotPrint(ref idCounter).Split('|')[1]}}}";
        }
        protected static string IndentHelper(
            string src, 
            int indentCount = 5, 
            bool drawSeperator = false, 
            char indentChar = ' ', 
            char seperatorChar = '│')
        {
            string[] split = src.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length > 1)
            {
                string result = split[0];
                for (int i = 1; i < split.Length; i++)
                {
                    string item = split[i];
                    if (drawSeperator)
                    {
                        result += Environment.NewLine + seperatorChar + new string(indentChar, indentCount - 1) + item;
                    }
                    else
                    {
                        result += Environment.NewLine + new string(indentChar, indentCount) + item;
                    }
                }
                return result;
            }
            else
            {
                return split[0];
            }
        }
        #endregion

        #region Serialization
        public static string SerializeJson(Node root)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            return JsonConvert.SerializeObject(root, settings);
        }
        public static Node DeserializeJson(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            return JsonConvert.DeserializeObject<StatementNode>(json, settings);
        }
        #endregion
    }
}
