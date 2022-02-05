using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniJSON
{
    public struct JsonPointer
    {
        public ArraySegment<String> Path
        {
            get;
            private set;
        }

        public string this[int index]
        {
            get
            {
                return Path.Array[Path.Offset + index];
            }
        }

        public JsonPointer Unshift()
        {
            return new JsonPointer
            {
                Path = new ArraySegment<string>(Path.Array, Path.Offset + 1, Path.Count - 1)
            };
        }

        public JsonPointer(JsonNode node)
        {
            Path = new ArraySegment<string>(node.Path().Skip(1).Select(x => GetKeyFromParent(x)).ToArray());
        }

        public JsonPointer(string pointer)
        {
            if (!pointer.StartsWith("/"))
            {
                throw new ArgumentException();
            }
            var splited = pointer.Split('/');
            Path = new ArraySegment<string>(splited, 1, splited.Length - 1);
        }

        public override string ToString()
        {
            if (Path.Count == 0)
            {
                return "/";
            }

            var sb = new StringBuilder();
            var end = Path.Offset + Path.Count;
            for (int i = Path.Offset; i < end; ++i)
            {
                sb.Append('/');
                sb.Append(Path.Array[i]);
            }
            return sb.ToString();
        }

        static string GetKeyFromParent(JsonNode json)
        {
            var parent = json.Parent;
            switch (parent.Value.ValueType)
            {
                case JsonValueType.Array:
                    {
                        return parent.IndexOf(json).ToString();
                    }

                case JsonValueType.Object:
                    {
                        return parent.KeyOf(json);
                    }

                default:
                    throw new NotImplementedException();
            }
        }
    }

    public enum JsonDiffType
    {
        KeyAdded,
        KeyRemoved,
        ValueChanged,
    }


    public struct JsonDiff
    {
        public JsonPointer Path;
        public JsonDiffType DiffType;
        public string Msg;

        public JsonDiff(JsonNode node, JsonDiffType diffType, string msg)
        {
            Path = new JsonPointer(node);
            DiffType = diffType;
            Msg = msg;
        }

        public override string ToString()
        {
            switch (DiffType)
            {
                case JsonDiffType.KeyAdded:
                    return string.Format("+ {0}: {1}", Path, Msg);
                case JsonDiffType.KeyRemoved:
                    return string.Format("- {0}: {1}", Path, Msg);
                case JsonDiffType.ValueChanged:
                    return string.Format("= {0}: {1}", Path, Msg);
                default:
                    throw new NotImplementedException();
            }
        }
    }


    public struct JsonNode
    {
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is JsonNode))
            {
                return false;
            }

            var rhs = (JsonNode)obj;

            if ((Value.ValueType == JsonValueType.Integer || Value.ValueType == JsonValueType.Null)
                && (rhs.Value.ValueType == JsonValueType.Integer || rhs.Value.ValueType == JsonValueType.Number))
            {
                // ok
            }
            else if (Value.ValueType != rhs.Value.ValueType)
            {
                return false;
            }

            switch (Value.ValueType)
            {
                case JsonValueType.Null:
                    return true;

                case JsonValueType.Boolean:
                    return Value.GetBoolean() == rhs.GetBoolean();

                case JsonValueType.Integer:
                case JsonValueType.Number:
                    return Value.GetDouble() == rhs.GetDouble();

                case JsonValueType.String:
                    return Value.GetString() == rhs.GetString();

                case JsonValueType.Array:
                    return ArrayItems.SequenceEqual(rhs.ArrayItems);

                case JsonValueType.Object:
                    {
                        var l = ObjectItems.ToDictionary(x => x.Key, x => x.Value);
                        var r = rhs.ObjectItems.ToDictionary(x => x.Key, x => x.Value);
                        l.Equals(r);
                        return ObjectItems.OrderBy(x => x.Key).SequenceEqual(rhs.ObjectItems.OrderBy(x => x.Key));
                    }
            }

            return false;
        }

        public IEnumerable<JsonDiff> Diff(JsonNode rhs, JsonPointer path = default(JsonPointer))
        {
            switch (Value.ValueType)
            {
                case JsonValueType.Null:
                case JsonValueType.Boolean:
                case JsonValueType.Number:
                case JsonValueType.Integer:
                case JsonValueType.String:
                    if (!Equals(rhs))
                    {
                        yield return new JsonDiff(this, JsonDiffType.ValueChanged, string.Format("{0} => {1}", Value, rhs.Value));
                    }
                    yield break;
            }

            if (Value.ValueType != rhs.Value.ValueType)
            {
                yield return new JsonDiff(this, JsonDiffType.ValueChanged, string.Format("{0} => {1}", Value.ValueType, rhs.Value));
                yield break;
            }

            if (Value.ValueType == JsonValueType.Object)
            {

                var l = ObjectItems.ToDictionary(x => x.Key, x => x.Value);
                var r = rhs.ObjectItems.ToDictionary(x => x.Key, x => x.Value);

                foreach (var kv in l)
                {
                    JsonNode x;
                    if (r.TryGetValue(kv.Key, out x))
                    {
                        r.Remove(kv.Key);
                        // Found
                        foreach (var y in kv.Value.Diff(x))
                        {
                            yield return y;
                        }
                    }
                    else
                    {
                        // Removed
                        yield return new JsonDiff(kv.Value, JsonDiffType.KeyRemoved, kv.Value.Value.ToString());
                    }
                }

                foreach (var kv in r)
                {
                    // Addded
                    yield return new JsonDiff(kv.Value, JsonDiffType.KeyAdded, kv.Value.Value.ToString());
                }
            }
            else if (Value.ValueType == JsonValueType.Array)
            {
                var ll = ArrayItems.GetEnumerator();
                var rr = rhs.ArrayItems.GetEnumerator();
                while (true)
                {
                    var lll = ll.MoveNext();
                    var rrr = rr.MoveNext();
                    if (lll && rrr)
                    {
                        // found
                        foreach (var y in ll.Current.Diff(rr.Current))
                        {
                            yield return y;
                        }
                    }
                    else if (lll)
                    {
                        yield return new JsonDiff(ll.Current, JsonDiffType.KeyRemoved, ll.Current.Value.ToString());
                    }
                    else if (rrr)
                    {
                        yield return new JsonDiff(rr.Current, JsonDiffType.KeyAdded, rr.Current.Value.ToString());
                    }
                    else
                    {
                        // end
                        break;
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public readonly List<JsonValue> Values;
        int m_index;
        public JsonValue Value
        {
            get { return Values[m_index]; }
        }
        public IEnumerable<JsonNode> Children
        {
            get
            {
                for (int i = 0; i < Values.Count; ++i)
                {
                    if (Values[i].ParentIndex == m_index)
                    {
                        yield return new JsonNode(Values, i);
                    }
                }
            }
        }
        public bool HasParent
        {
            get
            {
                return Value.ParentIndex >= 0 && Value.ParentIndex < Values.Count;
            }
        }
        public JsonNode Parent
        {
            get
            {
                if (Value.ParentIndex < 0)
                {
                    throw new Exception("no parent");
                }
                if (Value.ParentIndex >= Values.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                return new JsonNode(Values, Value.ParentIndex);
            }
        }

        public JsonNode(List<JsonValue> values, int index = 0)
        {
            Values = values;
            m_index = index;
        }

        #region object interface
        public JsonNode this[string key]
        {
            get
            {
                foreach (var kv in ObjectItems)
                {
                    if (kv.Key == key)
                    {
                        return kv.Value;
                    }
                }
                throw new KeyNotFoundException();
            }
        }
        public bool ContainsKey(string key)
        {
            return ObjectItems.Any(x => x.Key == key);
        }
        public IEnumerable<KeyValuePair<string, JsonNode>> ObjectItems
        {
            get
            {
                if (this.Value.ValueType != JsonValueType.Object) throw new JsonValueException("is not object");
                var it = Children.GetEnumerator();
                while (it.MoveNext())
                {
                    var key = it.Current.GetString();

                    it.MoveNext();
                    yield return new KeyValuePair<string, JsonNode>(key, it.Current);
                }
            }
        }
        public string KeyOf(JsonNode node)
        {
            foreach (var kv in ObjectItems)
            {
                if (node.m_index == kv.Value.m_index)
                {
                    return kv.Key;
                }
            }
            throw new KeyNotFoundException();
        }
        #endregion

        #region array interface
        public JsonNode this[int index]
        {
            get
            {
                int i = 0;
                foreach (var v in ArrayItems)
                {
                    if (i++ == index)
                    {
                        return v;
                    }
                }
                throw new KeyNotFoundException();
            }
        }
        public IEnumerable<JsonNode> ArrayItems
        {
            get
            {
                if (this.Value.ValueType != JsonValueType.Array) throw new JsonValueException("is not object");
                return Children;
            }
        }
        public int IndexOf(JsonNode child)
        {
            int i = 0;
            foreach (var v in ArrayItems)
            {
                if (v.m_index == child.m_index)
                {
                    return i;
                }
                ++i;
            }
            throw new KeyNotFoundException();
        }
        #endregion

        public void RemoveKey(string key)
        {
            if (Value.ValueType != JsonValueType.Object)
            {
                throw new Exception("is not object");
            }

            var parentIndex = m_index;
            var indices = Values
                .Select((value, index) => new { value, index })
                .Where(x => x.value.ParentIndex == parentIndex)
                .ToArray();

            for (int i = 0; i < indices.Length; i += 2)
            {
                if (indices[i].value.GetString() == key)
                {
                    Values[indices[i].index] = JsonValue.Empty; // remove
                    Values[indices[i + 1].index] = JsonValue.Empty; // remove
                }
            }
        }

        public void AddNode(string key, JsonNode node)
        {
            if (Value.ValueType != JsonValueType.Object)
            {
                throw new InvalidOperationException();
            }

            /*
            switch (key)
            {
                case "title":
                case "$schema":
                    // skip
                    return;
            }
            */

            Values.Add(new JsonValue(new StringSegment("\"" + key + "\""), JsonValueType.String, m_index));
            AddNode(node);
        }

        private void AddNode(JsonNode node)
        {
            var index = Values.Count;
            Values.Add(new JsonValue(node.Value.Segment, node.Value.ValueType, m_index));

            var parent = new JsonNode(Values, index);
            if (node.Value.ValueType == JsonValueType.Array)
            {
                foreach (var value in node.ArrayItems)
                {
                    parent.AddNode(value);
                }
            }
            else if (node.Value.ValueType == JsonValueType.Object)
            {
                foreach (var kv in node.ObjectItems)
                {
                    parent.AddNode(kv.Key, kv.Value);
                }
            }
        }

        public IEnumerable<JsonNode> GetNodes(JsonPointer jsonPointer)
        {
            if (jsonPointer.Path.Count == 0)
            {
                yield return this;
                yield break;
            }

            if (Value.ValueType == JsonValueType.Array)
            {
                // array
                if (jsonPointer[0] == "*")
                {
                    // wildcard
                    foreach (var child in ArrayItems)
                    {
                        foreach (var childChild in child.GetNodes(jsonPointer.Unshift()))
                        {
                            yield return childChild;
                        }
                    }
                }
                else
                {
                    int index;
                    if (!int.TryParse(jsonPointer[0], out index))
                    {
                        throw new KeyNotFoundException();
                    }
                    var child = this[index];
                    foreach (var childChild in child.GetNodes(jsonPointer.Unshift()))
                    {
                        yield return childChild;
                    }
                }
            }
            else if (Value.ValueType == JsonValueType.Object)
            {
                // object
                if (jsonPointer[0] == "*")
                {
                    // wildcard
                    foreach (var kv in ObjectItems)
                    {
                        foreach (var childChild in kv.Value.GetNodes(jsonPointer.Unshift()))
                        {
                            yield return childChild;
                        }
                    }
                }
                else
                {
                    JsonNode child;
                    try
                    {
                        child = this[jsonPointer[0]];
                    }
                    catch (KeyNotFoundException)
                    {
                        // key
                        Values.Add(new JsonValue(new StringSegment(JsonString.Quote(jsonPointer[0])), JsonValueType.String, m_index));
                        // value
                        Values.Add(new JsonValue(new StringSegment(), JsonValueType.Object, m_index));

                        child = this[jsonPointer[0]];
                    }
                    foreach (var childChild in child.GetNodes(jsonPointer.Unshift()))
                    {
                        yield return childChild;
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<JsonNode> GetNodes(string jsonPointer)
        {
            return GetNodes(new JsonPointer(jsonPointer));
        }

        void SetValue(JsonPointer jsonPointer, Func<int, JsonValue> createNodeValue)
        {
            foreach (var node in GetNodes(jsonPointer))
            {
                Values[node.m_index] = createNodeValue(node.Value.ParentIndex);
            }
        }

        public void SetValue(string jsonPointer, string value)
        {
            SetValue(new JsonPointer(jsonPointer), parentIndex => new JsonValue
            {
                ParentIndex = parentIndex,
                Segment = new StringSegment(JsonString.Quote(value)),
                ValueType = JsonValueType.String
            });
        }

        public void SetValue(string jsonPointer, int value)
        {
            SetValue(new JsonPointer(jsonPointer), parentIndex => new JsonValue
            {
                ParentIndex = parentIndex,
                Segment = new StringSegment(value.ToString()),
                ValueType = JsonValueType.Integer
            });
        }

        public void SetValue(string jsonPointer, float value)
        {
            SetValue(new JsonPointer(jsonPointer), parentIndex => new JsonValue
            {
                ParentIndex = parentIndex,
                Segment = new StringSegment(value.ToString()),
                ValueType = JsonValueType.Integer
            });
        }

        public void SetValue(string jsonPointer, bool value)
        {
            SetValue(new JsonPointer(jsonPointer), parentIndex => new JsonValue
            {
                ParentIndex = parentIndex,
                Segment = new StringSegment(value.ToString().ToLower()),
                ValueType = JsonValueType.Boolean
            });
        }

        public void RemoveValue(string jsonPointer)
        {
            foreach (var node in GetNodes(new JsonPointer(jsonPointer)))
            {
                if (node.Parent.Value.ValueType == JsonValueType.Object)
                {
                    Values[node.m_index - 1] = JsonValue.Empty; // remove key
                }
                Values[node.m_index] = JsonValue.Empty; // remove
            }
        }
    }

    public static class JsonNodeExtensions
    {
        public static Boolean GetBoolean(this JsonNode self)
        {
            return self.Value.GetBoolean();
        }

        public static Int32 GetInt32(this JsonNode self)
        {
            return self.Value.GetInt32();
        }

        public static Double GetDouble(this JsonNode self)
        {
            return self.Value.GetDouble();
        }

        public static string GetString(this JsonNode self)
        {
            return self.Value.GetString();
        }

        /*
        public static IEnumerable<KeyValuePair<string, JsonNode>> TraverseObjects(this JsonNode self)
        {
            foreach (var kv in self.ObjectItems)
            {
                yield return kv;

                if (kv.Value.Value.ValueType == JsonValueType.Object)
                {
                    foreach (var _kv in kv.Value.TraverseObjects())
                    {
                        yield return _kv;
                    }
                }
            }
        }
        */
        public static IEnumerable<JsonNode> Traverse(this JsonNode self)
        {
            yield return self;
            if (self.Value.ValueType == JsonValueType.Array)
            {
                foreach (var x in self.ArrayItems)
                {
                    foreach (var y in x.Traverse())
                    {
                        yield return y;
                    }
                }
            }
            else if (self.Value.ValueType == JsonValueType.Object)
            {
                foreach (var kv in self.ObjectItems)
                {
                    foreach (var y in kv.Value.Traverse())
                    {
                        yield return y;
                    }
                }
            }
        }

        public static IEnumerable<JsonNode> Path(this JsonNode self)
        {
            if (self.HasParent)
            {
                foreach (var x in self.Parent.Path())
                {
                    yield return x;
                }
            }

            yield return self;
        }
    }
}
