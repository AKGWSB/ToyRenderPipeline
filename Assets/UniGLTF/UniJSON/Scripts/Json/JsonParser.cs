using System;
using System.Collections.Generic;


namespace UniJSON
{
    public class JsonParseResult
    {
        public List<JsonValue> Values = new List<JsonValue>();
    }

    public static class JsonParser
    {
        static JsonValueType GetValueType(StringSegment segment)
        {
            switch (segment[0])
            {
                case '{': return JsonValueType.Object;
                case '[': return JsonValueType.Array;
                case '"': return JsonValueType.String;
                case 't': return JsonValueType.Boolean;
                case 'f': return JsonValueType.Boolean;
                case 'n': return JsonValueType.Null;

                case '-': // fall through
                case '0': // fall through
                case '1': // fall through
                case '2': // fall through
                case '3': // fall through
                case '4': // fall through
                case '5': // fall through
                case '6': // fall through
                case '7': // fall through
                case '8': // fall through
                case '9': // fall through
                    {
                        if (segment.IsInt)
                        {
                            return JsonValueType.Integer;
                        }
                        else
                        {
                            return JsonValueType.Number;
                        }
                    }

                default:
                    throw new JsonParseException(segment + " is not valid json start");
            }
        }

        static JsonValue ParsePrimitive(StringSegment segment, JsonValueType valueType, int parentIndex)
        {
            int i = 1;
            for (; i < segment.Count; ++i)
            {
                if (Char.IsWhiteSpace(segment[i])
                    || segment[i] == '}'
                    || segment[i] == ']'
                    || segment[i] == ','
                    || segment[i] == ':'
                    )
                {
                    break;
                }
            }
            return new JsonValue(segment.Take(i), valueType, parentIndex);
        }

        static JsonValue ParseString(StringSegment segment, int parentIndex)
        {
            int i = 1;
            for (; i < segment.Count; ++i)
            {
                if (segment[i] == '\"')
                {
                    return new JsonValue(segment.Take(i + 1), JsonValueType.String, parentIndex);
                }
                else if (segment[i] == '\\')
                {
                    switch (segment[i + 1])
                    {
                        case '"': // fall through
                        case '\\': // fall through
                        case '/': // fall through
                        case 'b': // fall through
                        case 'f': // fall through
                        case 'n': // fall through
                        case 'r': // fall through
                        case 't': // fall through
                                  // skip next
                            i += 1;
                            break;

                        case 'u': // unicode
                                  // skip next 4
                            i += 4;
                            break;

                        default:
                            // unkonw escape
                            throw new JsonParseException("unknown escape: " + segment.Skip(i));
                    }
                }
            }
            throw new JsonParseException("no close string: " + segment.Skip(i));
        }

        static StringSegment ParseArray(StringSegment segment, List<JsonValue> values, int parentIndex)
        {
            var closeChar = ']';
            bool isFirst = true;
            var current = segment.Skip(1);
            while (true)
            {
                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("no white space expected");
                    }
                    current = current.Skip(nextToken);
                }

                {
                    if (current[0] == closeChar)
                    {
                        // end
                        break;
                    }
                }

                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    // search ',' or closeChar
                    int keyPos;
                    if (!current.TrySearch(x => x == ',', out keyPos))
                    {
                        throw new JsonParseException("',' expected");
                    }
                    current = current.Skip(keyPos + 1);
                }

                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("not whitespace expected");
                    }
                    current = current.Skip(nextToken);
                }

                // value
                var value = Parse(current, values, parentIndex);
                current = current.Skip(value.Segment.Count);
            }

            return current;
        }

        static StringSegment ParseObject(StringSegment segment, List<JsonValue> values, int parentIndex)
        {
            var closeChar = '}';
            bool isFirst = true;
            var current = segment.Skip(1);
            while (true)
            {
                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("no white space expected");
                    }
                    current = current.Skip(nextToken);
                }

                {
                    if (current[0] == closeChar)
                    {
                        break;
                    }
                }

                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    // search ',' or closeChar
                    int keyPos;
                    if (!current.TrySearch(x => x == ',', out keyPos))
                    {
                        throw new JsonParseException("',' expected");
                    }
                    current = current.Skip(keyPos + 1);
                }

                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("not whitespace expected");
                    }
                    current = current.Skip(nextToken);
                }

                // key
                var key = Parse(current, values, parentIndex);
                if (key.ValueType != JsonValueType.String)
                {
                    throw new JsonParseException("object key must string: " + key.Segment);
                }
                current = current.Skip(key.Segment.Count);

                // search ':'
                int valuePos;
                if (!current.TrySearch(x => x == ':', out valuePos))
                {
                    throw new JsonParseException(": is not found");
                }
                current = current.Skip(valuePos + 1);

                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("not whitespace expected");
                    }
                    current = current.Skip(nextToken);
                }

                // value
                var value = Parse(current, values, parentIndex);
                current = current.Skip(value.Segment.Count);
            }

            return current;
        }

        public static JsonValue Parse(StringSegment segment, List<JsonValue> values, int parentIndex)
        {
            // skip white space
            int pos;
            if (!segment.TrySearch(x => !char.IsWhiteSpace(x), out pos))
            {
                throw new JsonParseException("only whitespace");
            }
            segment = segment.Skip(pos);

            var valueType = GetValueType(segment);
            switch (valueType)
            {
                case JsonValueType.Boolean:
                case JsonValueType.Integer:
                case JsonValueType.Number:
                case JsonValueType.Null:
                    {
                        var value= ParsePrimitive(segment, valueType, parentIndex);
                        values.Add(value);
                        return value;
                    }

                case JsonValueType.String:
                    {
                        var value= ParseString(segment, parentIndex);
                        values.Add(value);
                        return value;
                    }

                case JsonValueType.Array: // fall through
                    {
                        var index = values.Count;
                        values.Add(new JsonValue()); // placeholder
                        var current = ParseArray(segment, values, index);
                        values[index] = new JsonValue(new StringSegment(segment.Value, segment.Offset, current.Offset + 1 - segment.Offset),
                            JsonValueType.Array, parentIndex);
                        return values[index];
                    }

                case JsonValueType.Object: // fall through
                    {
                        var index = values.Count;
                        values.Add(new JsonValue()); // placeholder
                        var current=ParseObject(segment, values, index);
                        values[index] = new JsonValue(new StringSegment(segment.Value, segment.Offset, current.Offset + 1 - segment.Offset),
                            JsonValueType.Object, parentIndex);
                        return values[index];
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public static JsonNode Parse(String json)
        {
            var result = new List<JsonValue>();
            var value = Parse(new StringSegment(json), result, -1);
            if (value.ValueType != JsonValueType.Array && value.ValueType != JsonValueType.Object)
            {
                result.Add(value);
                return new JsonNode(result);
            }
            else
            {
                return new JsonNode(result);
            }
        }
    }
}
