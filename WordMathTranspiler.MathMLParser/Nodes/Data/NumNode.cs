namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class NumNode : Node
    {
        private object _value;
        public enum NumType {
            Empty,
            Int,
            Float
        }

        public NumType Type { 
            get {
                switch (_value)
                {
                    case int i:
                    case long l:
                        return NumType.Int;
                    case float f:
                    case double d:
                        return NumType.Float;
                    default:
                        return NumType.Empty;
                }
            } 
        }

        public object Value { 
            get {
                switch (Type)
                {
                    case NumType.Int:
                        return (long)_value;
                    case NumType.Float:
                        return (double)_value;
                    default:
                        return null;
                }
            }
            set {
                _value = value;
            }
        }
        /// <summary>
        /// Number node.
        /// </summary>
        /// <param name="value">Constant value</param>
        public NumNode(object value)
        {
            Value = value;
        }

        public override bool IsFloatPointOperation()
        {
            return Type == NumType.Float;
        }

        public override string Print()
        {
            return Value.ToString();
        }
    }
}
