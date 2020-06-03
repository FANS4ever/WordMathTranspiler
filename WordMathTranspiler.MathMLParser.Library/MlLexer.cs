using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace WordMathTranspiler.MathMLParser
{
    class MlLexer
    {
        private int index;
        private List<XElement> elementList;
        public bool IsFinished { get { return index >= elementList.Count; } }

        public int NodeCount { get { return elementList.Count; } }
        
        private MlLexerNodeInfo _node;
        public MlLexerNodeInfo Node { get { return _node; } }
       
        private XElement RawNode { get { return IsFinished ? null : elementList[index]; } } 

        public MlLexer(XElement root)
        {
            index = 0;
            elementList = root.Elements().ToList();
            _node = MlLexerNodeInfo.MakeNode(RawNode);
        }

        /// <summary>
        /// Move to the next node.
        /// </summary>
        /// <returns>Consumed node info.</returns>
        public MlLexerNodeInfo Eat(string nodeName)
        {
            if (Node.Name.Equals(nodeName))
            {
                MlLexerNodeInfo prev = Node;
                index++;
                _node = MlLexerNodeInfo.MakeNode(RawNode);
                return prev;
            }
            else
            {
                IXmlLineInfo lineInfo = GetLineInfo();
                throw new Exception($"[MlLexer] - Error in syntax. Tried to eat \"{nodeName}\" but current node is \"{Node.Name}\"" + (lineInfo.HasLineInfo() ? " Line:" + lineInfo.LineNumber : ""));
            }
        }

        /// <summary>
        /// Check the next node.
        /// </summary>
        /// <returns>Next node info or null if no more elements</returns>
        public MlLexerNodeInfo Peek()
        {
            XElement nextNode = index + 1 >= elementList.Count ? null : elementList[index + 1];
            return MlLexerNodeInfo.MakeNode(nextNode);
        }

        /// <summary>
        /// Get the IXmlLineInfo object from the current node.
        /// </summary>
        /// <returns>Line info of current node</returns>
        public IXmlLineInfo GetLineInfo()
        {
            return RawNode;
        }

        /// <summary>
        /// Create a new lexer with the root being the current node.
        /// </summary>
        /// <returns>MlLexer object</returns>
        public MlLexer GetDeepLexer()
        {
            if (IsFinished)
            {
                throw new Exception("[MlLexer] - Can't create deeper lexer. Lexer has finished working.");
            }
            return new MlLexer(RawNode);
        }
    }

    class MlLexerNodeInfo
    {
        public string Name { get; }
        public string Value { get; }
        public MlLexerNodeInfo(XElement node)
        {
            Name = node.Name.LocalName;
            Value = node.Value;
        }

        public static MlLexerNodeInfo MakeNode(XElement node)
        {
            return node == null ? null : new MlLexerNodeInfo(node);
        }
    }
}
