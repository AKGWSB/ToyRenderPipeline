using System;
using System.Linq.Expressions;

namespace UniJSON
{
    public interface IFormatter
    {
        IStore GetStore();

        IFormatter BeginList(int n);

        IFormatter EndList();

        IFormatter BeginMap(int n);

        IFormatter EndMap();

        IFormatter Key(string x);


        IFormatter Null();

        IFormatter Value(String x);

        IFormatter Value(ArraySegment<Byte> bytes);

        IFormatter Value(Boolean x);

        IFormatter Value(Byte x);
        IFormatter Value(UInt16 x);
        IFormatter Value(UInt32 x);
        IFormatter Value(UInt64 x);

        IFormatter Value(SByte x);
        IFormatter Value(Int16 x);
        IFormatter Value(Int32 x);
        IFormatter Value(Int64 x);

        IFormatter Value(Single x);
        IFormatter Value(Double x);
    }
}
