using NUnit.Framework;
using System;
using System.IO;

namespace UniJSON.MsgPack
{
    [TestFixture]
    public class BooleanTest
    {
        [Test]
        public void nil()
        {
            {
                var bytes = new MsgPackFormatter().Null().GetStore().Bytes;
                Assert.AreEqual(new Byte[] { 0xC0 }, bytes.ToEnumerable());

                var parsed = MsgPackParser.Parse(bytes);
                Assert.True(parsed.IsNull);
            }
        }

        [Test]
        public void True()
        {
            var bytes = new MsgPackFormatter().Value(true).GetStore().Bytes;
            Assert.AreEqual(new Byte[] { 0xC3 }, bytes.ToEnumerable());

            var value = MsgPackParser.Parse(bytes);
            var j = value.GetBoolean();
            Assert.AreEqual(true, j);
        }

        [Test]
        public void False()
        {
            var bytes = new MsgPackFormatter().Value(false).GetStore().Bytes;
            Assert.AreEqual(new Byte[] { 0xC2 }, bytes.ToEnumerable());

            var value = MsgPackParser.Parse(bytes);
            var j = value.GetBoolean();
            Assert.AreEqual(false, j);
        }
    }
}
