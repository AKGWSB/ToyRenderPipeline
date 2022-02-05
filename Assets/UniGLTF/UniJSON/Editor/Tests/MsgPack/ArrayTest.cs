using NUnit.Framework;
using System;
using System.Linq;

namespace UniJSON.MsgPack
{
    [TestFixture]
    public class ArrayTest
    {
        [Test]
        public void fix_array()
        {
            var f = new MsgPackFormatter();
            f.Value(new[] { 0, 1, false, (Object)null });
            var bytes = f.GetStore().Bytes;

            Assert.AreEqual(new Byte[]{
                (Byte)MsgPackType.FIX_ARRAY_0x4,
                (Byte)MsgPackType.POSITIVE_FIXNUM,
                (Byte)MsgPackType.POSITIVE_FIXNUM_0x01,
                (Byte)MsgPackType.FALSE,
                (Byte)MsgPackType.NIL
            }, bytes.ToEnumerable());

            var parsed = MsgPackParser.Parse(bytes);

            Assert.AreEqual(4, parsed.Count);
            Assert.AreEqual(0, parsed[0].GetValue());
            Assert.AreEqual(1, parsed[1].GetValue());
            Assert.False((Boolean)parsed[2].GetValue());
            Assert.AreEqual(null, parsed[3].GetValue());
        }

        [Test]
        public void array16()
        {
            var f = new MsgPackFormatter();
            var data = Enumerable.Range(0, 20).Select(x => (Object)x).ToArray();
            f.Value(data);
            var bytes = f.GetStore().Bytes;

            var value = MsgPackParser.Parse(bytes);
            Assert.IsTrue(value.IsArray);
            Assert.AreEqual(20, value.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, value[i].GetValue());
            }
        }

        [Test]
        public void array129()
        {
            {
                var i128 = Enumerable.Range(0, 128).ToArray();
                var bytes128 = new MsgPackFormatter().Value(i128).GetStore().Bytes;
                var deserialized = MsgPackParser.Parse(bytes128);
                Assert.AreEqual(128, deserialized.Count);
                for (int i = 0; i < i128.Length; ++i)
                {
                    Assert.AreEqual(i128[i], deserialized[i].GetValue());
                }
            }

            {
                var i129 = Enumerable.Range(0, 129).ToArray();
                var bytes129 = new MsgPackFormatter().Value(i129).GetStore().Bytes;
                var deserialized = MsgPackParser.Parse(bytes129);
                Assert.AreEqual(129, deserialized.Count);
                for (int i = 0; i < i129.Length; ++i)
                {
                    Assert.AreEqual(i129[i], deserialized[i].GetValue());
                }
            }
        }
    }
}
