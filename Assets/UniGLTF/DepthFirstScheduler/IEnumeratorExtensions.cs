﻿using System.Collections;
using System.Collections.Generic;


namespace
    DepthFirstScheduler
{
    public static class IEnumeratorExtensions
    {
        public static void CoroutinetoEnd(this IEnumerator coroutine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(coroutine);
            while (stack.Count > 0)
            {
                if (stack.Peek().MoveNext())
                {
                    var nested = stack.Peek().Current as IEnumerator;
                    if (nested != null)
                    {
                        stack.Push(nested);
                    }
                }
                else
                {
                    stack.Pop();
                }
            }
        }
    }
}
