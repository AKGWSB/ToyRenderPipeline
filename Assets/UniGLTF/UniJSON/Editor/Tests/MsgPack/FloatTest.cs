using NUnit.Framework;
using System;
using System.Linq;


namespace UniJSON.MsgPack
{
    [TestFixture]
    public class FloatTest
    {
        [Test]
        public void Float32()
        {
            var i = 1.1f;
            var float_be = new byte[]
            {
                0x3f, 0x8c, 0xcc, 0xcd
            };

            var bytes = new MsgPackFormatter().Value(i).GetStore().Bytes;

            var value = MsgPackParser.Parse(bytes);
            var body = value.GetBody();
            Assert.AreEqual(float_be, body.ToEnumerable().ToArray());

            Assert.AreEqual(i, value.GetValue());
        }

        [Test]
        public void Float64()
        {
            var i = 1.1;
            var double_be = new Byte[]{
                0x3f, 0xf1, 0x99, 0x99, 0x99, 0x99, 0x99, 0x9a,
            };

            var bytes = new MsgPackFormatter().Value(i).GetStore().Bytes;

            var value = MsgPackParser.Parse(bytes);
            var body = value.GetBody();
            Assert.AreEqual(double_be, body.ToEnumerable().ToArray());

            Assert.AreEqual(i, value.GetValue());
        }
    }
}
