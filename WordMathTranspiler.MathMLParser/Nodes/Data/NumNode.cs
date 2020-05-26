using Newtonsoft.Json;

namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class NumNode : Node
    {
        public enum NumType
        {
            Empty,
            Int,
            Float
        }

        private object _value;
        
        [JsonIgnore]
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

        public override bool Equals(object obj)
        {
            NumNode item = obj as NumNode;

            if (item == null)
            {
                return false;
            }

            return item.Value.Equals(Value);
        }

        public override int GetHashCode()
        {
            return (Value).GetHashCode();
        }
    }
}
