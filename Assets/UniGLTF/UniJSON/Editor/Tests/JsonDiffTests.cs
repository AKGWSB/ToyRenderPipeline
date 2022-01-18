using NUnit.Framework;
using System.Linq;


namespace UniJSON
{
    public class JsonDiffTests
    {
        [Test]
        public void PathTest()
        {
            var json=@"
{
    ""a"": [
        {
            ""aa"": 1
        }       
    ]
}
";
            var root = JsonParser.Parse(json);

            {
                var it = root.Traverse().GetEnumerator();
                it.MoveNext(); Assert.AreEqual("/", new JsonPointer(it.Current).ToString());
                it.MoveNext(); Assert.AreEqual("/a", new JsonPointer(it.Current).ToString());
                it.MoveNext(); Assert.AreEqual("/a/0", new JsonPointer(it.Current).ToString());
                it.MoveNext(); Assert.AreEqual("/a/0/aa", new JsonPointer(it.Current).ToString());
                Assert.False(it.MoveNext());
            }

            {
                var it = root.Traverse().GetEnumerator();
                root.SetValue("/a", "JsonPath");
                it.MoveNext(); Assert.AreEqual("/", new JsonPointer(it.Current).ToString());
                it.MoveNext(); Assert.AreEqual("/a", new JsonPointer(it.Current).ToString());
                Assert.False(it.MoveNext());
            }
        }

        [Test]
        public void DiffTest()
        {
            var a = @"{
""a"": 1
}";

            var b = @"{
}";

            var diff = JsonParser.Parse(a).Diff(JsonParser.Parse(b)).ToArray();
            Assert.AreEqual(1, diff.Length);
        }

        [Test]
        public void Vector3()
        {
            var src = new UnityEngine.Vector3(1, 2, 3);
            var json = UnityEngine.JsonUtility.ToJson(src);
            Assert.AreEqual("{\"x\":1.0,\"y\":2.0,\"z\":3.0}", json);
            var dst = UnityEngine.JsonUtility.FromJson<UnityEngine.Vector3>(json);
            Assert.AreEqual(src, dst);
        }
    }
}
