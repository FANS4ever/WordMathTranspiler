using System.Collections.Generic;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    class FuncDeclNode: Node
    {
        public string Name { get; set; }
        public List<Node> Params { get; set; }
        public Node Body { get; set; }
        public FuncDeclNode(string name, Node param, Node body)
        {
            Name = name;
            Params = new List<Node>() { param };
            Body = body;
        }
        public FuncDeclNode(string name, List<Node> paramList, Node body)
        {
            Name = name;
            Params = paramList;
            Body = body;
        }

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            // For now assume that its true
            return true;
        }
        public override string PrintHelper()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ " + $"Func: {Name}");
            for (int i = 0; i < Params.Count; i++)
            {
                sb.AppendLine("├─Param" + i + ": " + IndentHelper(Params[i].PrintHelper(), indentCount: 9 + i.ToString().Length, drawSeperator: true));
            }
            sb.Append("└─Body: " + IndentHelper(Body.PrintHelper(), 8));
            return sb.ToString();
        }
        public override string DotHelper(ref int id)
        {
            string fdeclId = $"funcDecl{id++}";
            string fdeclDecl = $"{fdeclId}[label=\"FuncDecl:{Name}\"]\n";
            string fdeclResult = fdeclDecl;
            for (int i = 0; i < Params.Count; i++)
            {
                var paramData = Params[i].DotHelper(ref id).Split('|');
                fdeclResult += $"{paramData[1]}{fdeclId} -> {paramData[0]}[label=\"Param{i}\"];\n";
            }
            var fdeclBody = Body.DotHelper(ref id).Split('|');
            return $"{fdeclId}|{fdeclResult}{fdeclBody[1]}{fdeclId} -> {fdeclBody[0]}[label=\"Body\"];\n";
        }
        #endregion

        public override bool Equals(object obj)
        {
            FuncDeclNode item = obj as FuncDeclNode;

            if (item == null)
            {
                return false;
            }

            if (Params.Count == item.Params.Count)
            {
                for (int i = 0; i < Params.Count; i++)
                {
                    if (!Params[i].Equals(item.Params[i]))
                    {
                        return false;
                    }
                }
                return Name.Equals(item.Name) && Body.Equals(item.Body);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Params.GetHashCode() ^ Body.GetHashCode();
        }
    }
}
