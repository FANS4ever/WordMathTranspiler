using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes
{
    /// <summary>
    /// Base node class
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// Adds indentation to multiline strings.
        /// </summary>
        /// <param name="src">Input string</param>
        /// <param name="count">Indentation level (spaces). Default: 5</param>
        /// <param name="vSeperator">Should the method use the '│' vertical seperator. Default: false</param>
        /// <returns></returns>
        protected static string IndentHelper(string src, int count = 5, bool vSeperator = false)
        {
            string[] split = src.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            
            if (split.Length > 1)
            {
                string result = split[0];
                for (int i = 1; i < split.Length; i++)
                {
                    string item = split[i];
                    if (vSeperator)
                    {
                        result += Environment.NewLine + '│' + new string(' ', count - 1) + item;
                    }
                    else
                    {
                        result += Environment.NewLine + new string(' ', count) + item;
                    }
                }
                return result;
            }
            else
            {
                return split[0];
            }
        }
        public abstract string Print();
        public abstract bool IsFloatPointOperation();
    }
}
