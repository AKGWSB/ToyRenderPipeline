using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using UnityEngine;

namespace UniJSON
{
    public static partial class FormatterExtensions
    {
        public static IFormatter Value(this IFormatter f, object x)
        {
            if (x == null)
            {
                f.Null();
                return f;
            }

            var t = x.GetType();
            if (t == typeof(Boolean))
            {
                f.Value((Boolean)x);
            }
            else if (t == typeof(SByte))
            {
                f.Value((SByte)x);
            }
            else if (t == typeof(Int16))
            {
                f.Value((Int16)x);
            }
            else if (t == typeof(Int32))
            {
                f.Value((Int32)x);
            }
            else if (t == typeof(Int64))
            {
                f.Value((Int64)x);
            }
            else if (t == typeof(Byte))
            {
                f.Value((Byte)x);
            }
            else if (t == typeof(UInt16))
            {
                f.Value((UInt16)x);
            }
            else if (t == typeof(UInt32))
            {
                f.Value((UInt32)x);
            }
            else if (t == typeof(UInt64))
            {
                f.Value((UInt64)x);
            }
            else if (t == typeof(Single))
            {
                f.Value((Single)x);
            }
            else if (t == typeof(Double))
            {
                f.Value((Double)x);
            }
            else if (t == typeof(String))
            {
                f.Value((String)x);
            }
            else
            {
                throw new NotImplementedException();
            }
            return f;
        }

        public static IFormatter Value(this IFormatter f, object[] a)
        {
            f.BeginList(a.Length);
            foreach (var x in a)
            {
                f.Value(x);
            }
            f.EndList();
            return f;
        }

        static Action<T> GetValueMethod<T>(this IFormatter f)
        {
            var mi = typeof(IFormatter).GetMethods().First(x =>
            {
                if (x.Name != "Value")
                {
                    return false;
                }
                var args = x.GetParameters();
                return args.Length == 1 && args[0].ParameterType == typeof(T);
            });
            return t =>
            {

                mi.Invoke(f, new object[] { t });

            };
        }

        public static IFormatter Value<T>(this IFormatter f, T[] a)
        {
            f.BeginList(a.Length);
            var method = f.GetValueMethod<T>();
            foreach (var x in a)
            {
                method(x);
            }
            f.EndList();
            return f;
        }

        public static IFormatter Value(this IFormatter f, List<object> a)
        {
            f.BeginList(a.Count);
            foreach (var x in a)
            {
                f.Value(x);
            }
            f.EndList();
            return f;
        }

        public static IFormatter Value(this IFormatter f, Byte[] value)
        {
            return f.Value(new ArraySegment<Byte>(value));
        }

        public static IFormatter Value(this IFormatter f, Vector3 v)
        {
            //CommaCheck();
            f.BeginMap(3);
            f.Key("x"); f.Value(v.x);
            f.Key("y"); f.Value(v.y);
            f.Key("z"); f.Value(v.z);
            f.EndMap();
            return f;
        }

        public static void KeyValue<T>(this IFormatter f, Expression<Func<T>> expression)
        {
            var func = expression.Compile();
            var value = func();
            if (value != null)
            {
                var body = expression.Body as MemberExpression;
                if (body == null)
                {
                    body = ((UnaryExpression)expression.Body).Operand as MemberExpression;
                }
                f.Key(body.Member.Name);

                f.Value(value);
            }
        }

        public static ActionDisposer BeginListDisposable(this JsonFormatter f)
        {
            f.BeginList();
            return new ActionDisposer(() => f.EndList());
        }

        public static ActionDisposer BeginMapDisposable(this JsonFormatter f)
        {
            f.BeginMap();
            return new ActionDisposer(() => f.EndMap());
        }
    }
}
