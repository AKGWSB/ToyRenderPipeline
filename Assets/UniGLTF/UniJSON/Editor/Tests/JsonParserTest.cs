using NUnit.Framework;
using System;
using System.Linq;


namespace UniJSON
{
    public class JsonParserTest
    {
        [Test]
        public void Tests()
        {
            {
                var result = JsonParser.Parse("1");
                Assert.AreEqual(1, result.GetInt32());
            }

            {
                var result = JsonParser.Parse("{ \"a\": { \"b\": 1 }}");
                Assert.True(result.ContainsKey("a"));
            }
        }

        [Test]
        public void NullTest()
        {
            {
                var node = JsonParser.Parse("null");
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(4, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Null, node.Value.ValueType);
            }
        }

        [Test]
        public void BooleanTest()
        {
            {
                var node = JsonParser.Parse("true");
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(4, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Boolean, node.Value.ValueType);
                Assert.AreEqual(true, node.GetBoolean());
                Assert.Catch(typeof(FormatException), () => node.GetDouble());
            }
            {
                var node = JsonParser.Parse(" false ");
                Assert.AreEqual(1, node.Value.Segment.Offset);
                Assert.AreEqual(5, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Boolean, node.Value.ValueType);
                Assert.AreEqual(false, node.GetBoolean());
            }
        }

        [Test]
        public void NumberTest()
        {
            {
                var node = JsonParser.Parse("1");
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(1, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Integer, node.Value.ValueType);
                Assert.AreEqual(1, (int)node.GetDouble());
                Assert.Catch(typeof(JsonValueException), () => node.GetBoolean());
            }
            {
                var node = JsonParser.Parse(" 22 ");
                Assert.AreEqual(1, node.Value.Segment.Offset);
                Assert.AreEqual(2, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Integer, node.Value.ValueType);
                Assert.AreEqual(22, (int)node.GetDouble());
            }
            {
                var node = JsonParser.Parse(" 3.3 ");
                Assert.AreEqual(1, node.Value.Segment.Offset);
                Assert.AreEqual(3, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Number, node.Value.ValueType);
                Assert.AreEqual(3, (int)node.GetDouble());
                Assert.AreEqual(3.3f, (float)node.GetDouble());
            }
            {
                var node = JsonParser.Parse(" -4.44444444444444444444 ");
                Assert.AreEqual(JsonValueType.Number, node.Value.ValueType);
                Assert.AreEqual(-4, (int)node.GetDouble());
                Assert.AreEqual(-4.44444444444444444444, node.GetDouble());
            }
            {
                var node = JsonParser.Parse(" -5e-4 ");
                Assert.AreEqual(JsonValueType.Number, node.Value.ValueType);
                Assert.AreEqual(0, (int)node.GetDouble());
                Assert.AreEqual(-5e-4, node.GetDouble());
            }
        }

        [Test]
        public void StringTest()
        {
            {
                var value = "hoge";
                var quoted = "\"hoge\"";
                Assert.AreEqual(quoted, JsonString.Quote(value));
                var node = JsonParser.Parse(quoted);
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(quoted.Length, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.String, node.Value.ValueType);
                Assert.AreEqual("hoge", node.GetString());
            }

            {
                var value = "fuga\n  hoge";
                var quoted = "\"fuga\\n  hoge\"";
                Assert.AreEqual(quoted, JsonString.Quote(value));
                var node = JsonParser.Parse(quoted);
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(quoted.Length, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.String, node.Value.ValueType);
                Assert.AreEqual(value, node.GetString());
            }
        }

        [Test]
        public void StringEscapeTest()
        {
            {
                var value = "\"";
                var escaped = "\\\"";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "\\";
                var escaped = "\\\\";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "/";
                var escaped = "\\/";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "\b";
                var escaped = "\\b";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "\f";
                var escaped = "\\f";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "\n";
                var escaped = "\\n";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "\r";
                var escaped = "\\r";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
            {
                var value = "\t";
                var escaped = "\\t";
                Assert.AreEqual(escaped, JsonString.Escape(value));
                Assert.AreEqual(value, JsonString.Unescape(escaped));
            }
        }

        [Test]
        public void ObjectTest()
        {
            {
                var json = "{}";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(0, node.Value.Segment.Offset);

                Assert.AreEqual(2, node.Value.Segment.Count);

                Assert.AreEqual(JsonValueType.Object, node.Value.ValueType);
                Assert.AreEqual(0, node.ObjectItems.Count());
            }

            {
                var json = "{\"key\":\"value\"}";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(json.Length, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Object, node.Value.ValueType);

                var it = node.ObjectItems.GetEnumerator();

                Assert.IsTrue(it.MoveNext());
                Assert.AreEqual("key", it.Current.Key);
                Assert.AreEqual("value", it.Current.Value.GetString());

                Assert.IsFalse(it.MoveNext());
            }

            {
                var json = "{\"key\":\"value\"}";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(0, node.Value.Segment.Offset);
                Assert.AreEqual(json.Length, node.Value.Segment.Count);
                Assert.AreEqual(JsonValueType.Object, node.Value.ValueType);

                var it = node.ObjectItems.GetEnumerator();

                Assert.IsTrue(it.MoveNext());
                Assert.AreEqual("key", it.Current.Key);
                Assert.AreEqual("value", it.Current.Value.GetString());

                Assert.IsFalse(it.MoveNext());
            }
        }

        [Test]
        public void NestedObjectTest()
        {
            {
                var json = "{\"key\":{ \"nestedKey\": \"nestedValue\" }, \"key2\": { \"nestedKey2\": \"nestedValue2\" } }";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(JsonValueType.Object, node.Value.ValueType);

                {
                    var it = node.ObjectItems.GetEnumerator();

                    Assert.IsTrue(it.MoveNext());
                    Assert.AreEqual("key", it.Current.Key);
                    Assert.AreEqual(JsonValueType.Object, it.Current.Value.Value.ValueType);

                    Assert.IsTrue(it.MoveNext());
                    Assert.AreEqual("key2", it.Current.Key);
                    Assert.AreEqual(JsonValueType.Object, it.Current.Value.Value.ValueType);

                    Assert.IsFalse(it.MoveNext());
                }

                var nested = node["key2"];

                {
                    var it = nested.ObjectItems.GetEnumerator();

                    Assert.IsTrue(it.MoveNext());
                    Assert.AreEqual("nestedKey2", it.Current.Key);
                    Assert.AreEqual("nestedValue2", it.Current.Value.GetString());

                    Assert.IsFalse(it.MoveNext());
                }

                Assert.AreEqual("nestedValue2", node["key2"]["nestedKey2"].GetString());
            }
        }

        [Test]
        public void ArrayTest()
        {
            {
                var json = "[]";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(0, node.Value.Segment.Offset);

                //Assert.Catch(() => { var result = node.Value.Segment.Count; }, "raise exception");
                Assert.AreEqual(2, node.Value.Segment.Count);

                Assert.AreEqual(JsonValueType.Array, node.Value.ValueType);
            }

            {
                var json = "[1,2,3]";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(0, node.Value.Segment.Offset);

                //Assert.Catch(() => { var result = node.Value.Segment.Count; }, "raise exception");

                Assert.AreEqual(JsonValueType.Array, node.Value.ValueType);
                Assert.AreEqual(1, node[0].GetDouble());
                Assert.AreEqual(2, node[1].GetDouble());
                Assert.AreEqual(3, node[2].GetDouble());
            }

            {
                var json = "[\"key\",1]";
                var node = JsonParser.Parse(json);
                Assert.AreEqual(0, node.Value.Segment.Offset);

                //Assert.Catch(() => { var result = node.Value.Segment.Count; }, "raise exception");
                Assert.AreEqual(json.Length, node.Value.Segment.Count);

                Assert.AreEqual(JsonValueType.Array, node.Value.ValueType);

                var it = node.ArrayItems.GetEnumerator();

                Assert.IsTrue(it.MoveNext());
                Assert.AreEqual("key", it.Current.GetString());

                Assert.IsTrue(it.MoveNext());
                Assert.AreEqual(1, it.Current.GetDouble());

                Assert.IsFalse(it.MoveNext());

                Assert.AreEqual("key", node[0].GetString());
                Assert.AreEqual(1, node[1].GetDouble());
            }
        }

        [Test]
        public void ParseTest()
        {
            var json = "{";
            Assert.Catch(typeof(JsonParseException), () => JsonParser.Parse(json));
        }
    }
}
