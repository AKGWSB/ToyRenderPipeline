using System;
using System.Collections.Generic;
using System.Linq;


namespace UniJSON
{
    public static class JsonEnumValidator
    {
        public static IJsonSchemaValidator Create(JsonNode value)
        {
            foreach (var x in value.ArrayItems)
            {
                switch (x.Value.ValueType)
                {
                    case JsonValueType.Integer:
                    case JsonValueType.Number:
                        return JsonIntEnumValidator.Create(value.ArrayItems
                            .Where(y => y.Value.ValueType == JsonValueType.Integer || y.Value.ValueType == JsonValueType.Number)
                            .Select(y => y.GetInt32())
                            );

                    case JsonValueType.String:
                        return JsonStringEnumValidator.Create(value.ArrayItems
                            .Where(y => y.Value.ValueType == JsonValueType.String)
                            .Select(y => y.GetString())
                            );

                    default:
                        break;
                }
            }

            throw new NotImplementedException();
        }

        public static IJsonSchemaValidator Create(IEnumerable<JsonSchema> composition)
        {
            foreach (var x in composition)
            {
                if (x.Validator is JsonStringEnumValidator)
                {
                    return JsonStringEnumValidator.Create(composition
                        .Select(y => y.Validator as JsonStringEnumValidator)
                        .Where(y => y != null)
                        .SelectMany(y => y.Values)
                        );
                }
                if (x.Validator is JsonIntEnumValidator)
                {
                    return JsonIntEnumValidator.Create(composition
                        .Select(y => y.Validator as JsonIntEnumValidator)
                        .Where(y => y != null)
                        .SelectMany(y => y.Values)
                        );
                }
            }

            throw new NotImplementedException();
        }

        static IEnumerable<string> GetStringValues(Type t, object[] excludes, Func<String, String> filter)
        {
            foreach (var x in Enum.GetValues(t))
            {
                if (excludes == null || !excludes.Contains(x))
                {
                    yield return filter(x.ToString());
                }
            }
        }

        static IEnumerable<int> GetIntValues(Type t, object[] excludes)
        {
            foreach (var x in Enum.GetValues(t))
            {
                if (excludes == null || !excludes.Contains(x))
                {
                    yield return (int)x;
                }
            }
        }

        public static IJsonSchemaValidator Create(Type t, EnumSerializationType serializationType, object[] excludes)
        {
            switch (serializationType)
            {
                case EnumSerializationType.AsLowerString:
                    return JsonStringEnumValidator.Create(GetStringValues(t, excludes, x => x.ToLower()));

                case EnumSerializationType.AsInt:
                    return JsonIntEnumValidator.Create(GetIntValues(t, excludes));

                default:
                    throw new NotImplementedException();
            }
        }

        public static IJsonSchemaValidator Create(object[] values)
        {
            foreach (var x in values)
            {
                if (x is string)
                {
                    return JsonStringEnumValidator.Create(values.Select(y => (string)y));
                }
                if (x is int)
                {
                    return JsonIntEnumValidator.Create(values.Select(y => (int)y));
                }
            }

            throw new NotImplementedException();
        }
    }

    public class JsonStringEnumValidator : IJsonSchemaValidator
    {
        public String[] Values
        {
            get; set;
        }

        public static JsonStringEnumValidator Create(IEnumerable<string> values)
        {
            return new JsonStringEnumValidator
            {
                Values = values.ToArray(),
            };
        }

        public override int GetHashCode()
        {
            return 7;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as JsonStringEnumValidator;
            if (rhs == null) return false;

            if (Values.Length != rhs.Values.Length) return false;

            var l = Values.OrderBy(x => x).GetEnumerator();
            var r = rhs.Values.OrderBy(x => x).GetEnumerator();
            while (l.MoveNext() && r.MoveNext())
            {
                if (l.Current != r.Current)
                {
                    return false;
                }
            }
            return true;
        }

        public void Assign(IJsonSchemaValidator obj)
        {
            throw new NotImplementedException();
        }

        public bool Parse(IFileSystemAccessor fs, string key, JsonNode value)
        {
            throw new NotImplementedException();
        }

        public JsonSchemaValidationException Validate(JsonSchemaValidationContext c, object o)
        {
            if (o == null)
            {
                return new JsonSchemaValidationException(c, "null");
            }

            var t = o.GetType();
            string value = null;
            if (t.IsEnum)
            {
                value = Enum.GetName(t, o);
            }
            else
            {
                value = (string)o;
            }

            if (Values.Contains(value))
            {
                return null;
            }
            else
            {
                return new JsonSchemaValidationException(c, string.Format("{0} is not valid enum", o));
            }
        }

        public void Serialize(JsonFormatter f, JsonSchemaValidationContext c, object o)
        {
            f.Value((string)o);
        }

        public void ToJson(JsonFormatter f)
        {
            f.Key("type"); f.Value("string");
            f.Key("enum");
            f.BeginList();
            foreach (var x in Values)
            {
                f.Value(x);
            }
            f.EndList();
        }
    }

    public class JsonIntEnumValidator : IJsonSchemaValidator
    {
        public int[] Values
        {
            get; set;
        }

        public static JsonIntEnumValidator Create(IEnumerable<int> values)
        {
            return new JsonIntEnumValidator
            {
                Values = values.ToArray()
            };
        }

        public override int GetHashCode()
        {
            return 7;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as JsonIntEnumValidator;
            if (rhs == null) return false;

            if (Values.Length != rhs.Values.Length) return false;

            var l = Values.OrderBy(x => x).GetEnumerator();
            var r = rhs.Values.OrderBy(x => x).GetEnumerator();
            while (l.MoveNext() && r.MoveNext())
            {
                if (l.Current != r.Current)
                {
                    return false;
                }
            }
            return true;
        }

        public void Assign(IJsonSchemaValidator obj)
        {
            throw new NotImplementedException();
        }

        public bool Parse(IFileSystemAccessor fs, string key, JsonNode value)
        {
            throw new NotImplementedException();
        }

        public JsonSchemaValidationException Validate(JsonSchemaValidationContext c, object o)
        {
            if (Values.Contains((int)o))
            {
                return null;
            }
            else
            {
                return new JsonSchemaValidationException(c, string.Format("{0} is not valid enum", o));
            }
        }

        public void Serialize(JsonFormatter f, JsonSchemaValidationContext c, object o)
        {
            f.Value((int)o);
        }

        public void ToJson(JsonFormatter f)
        {
            f.Key("type"); f.Value("integer");
        }
    }
}
