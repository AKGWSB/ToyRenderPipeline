using System;
using System.Collections;

namespace UniJSON
{
    public static class ObjectExtensions
    {
        public static Object GetValueByKey(this Object self, String key)
        {
            var t = self.GetType();
            var fi = t.GetField(key);
            if (fi != null)
            {
                return fi.GetValue(self);
            }

            var pi = t.GetProperty(key);
            if (pi != null)
            {
                return fi.GetValue(self);
            }

            throw new ArgumentException();
        }

        public static int GetCount(this object self)
        {
            var count = 0;
            foreach (var x in self as IEnumerable)
            {
                ++count;
            }
            return count;
        }
    }
}
