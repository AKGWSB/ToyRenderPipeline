using System;
using System.Globalization;


namespace UniJSON
{
    public enum JsonValueType
    {
        Unknown,

        Null,
        Boolean,

        Number,
        String,

        Object,
        Array,

        Integer, // JsonSchema

        //Close, // internal use
    }

    public struct JsonValue
    {
        public StringSegment Segment;
        public JsonValueType ValueType;
        public int ParentIndex;

        public JsonValue(StringSegment segment, JsonValueType valueType, int parentIndex)
        {
            Segment = segment;
            ValueType = valueType;
            ParentIndex = parentIndex;
            //UnityEngine.Debug.LogFormat("{0}", this.ToString());
        }

        public static readonly JsonValue Empty = new JsonValue
        {
            ParentIndex = -1
        };

        public override string ToString()
        {
            //return "[" + ParentIndex + "]" + ValueType + ": " + Segment.ToString();
            switch (ValueType)
            {
                case JsonValueType.Null: 
                case JsonValueType.Boolean:
                case JsonValueType.Integer:
                case JsonValueType.Number:
                case JsonValueType.Array:
                case JsonValueType.Object:
                    return Segment.ToString();

                case JsonValueType.String:
                    return GetString();

                default:
                    throw new NotImplementedException();
            }
        }

        public Boolean GetBoolean()
        {
            var s = Segment.ToString();
            if (s == "true")
            {
                return true;
            }
            else if (s == "false")
            {
                return false;
            }
            else
            {
                throw new JsonValueException("invalid boolean: " + s);
            }
        }

        public Int32 GetInt32()
        {
            return Int32.Parse(Segment.ToString());
        }

        public Single GetSingle()
        {
            return Single.Parse(Segment.ToString(), CultureInfo.InvariantCulture);
        }

        public Double GetDouble()
        {
            return Double.Parse(Segment.ToString(), CultureInfo.InvariantCulture);
        }

        public String GetString()
        {
            var quoted = Segment.ToString();
            return JsonString.Unquote(quoted);
        }
    }
}
