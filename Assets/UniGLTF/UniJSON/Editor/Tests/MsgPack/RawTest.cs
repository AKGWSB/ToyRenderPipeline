using NUnit.Framework;
using System;
using System.Linq;

namespace UniJSON.MsgPack
{
    [TestFixture]
    public class RawTest
    {
        [Test]
        public void fix_raw()
        {
            var src = new Byte[] { 0, 1, 2 };
            var bytes = new MsgPackFormatter().Value(src).GetStore().Bytes;

            var v = MsgPackParser.Parse(bytes).GetBytes();
            Assert.True(src.SequenceEqual(v.ToEnumerable()));
        }

        [Test]
        public void raw16()
        {
            var src = Enumerable.Range(0, 50).Select(x => (Byte)x).ToArray();
            var bytes = new MsgPackFormatter().Value(src).GetStore().Bytes;

            var v = MsgPackParser.Parse(bytes).GetBytes();
            Assert.True(src.SequenceEqual(v.ToEnumerable()));
        }
    }
}
