namespace UniJSON
{
    public interface IValueNode
    {
        bool IsNull { get; }

        bool IsArray { get; }
        bool IsMap { get; }
        int Count { get; }

        object GetValue();
        bool GetBoolean();
    }
}
