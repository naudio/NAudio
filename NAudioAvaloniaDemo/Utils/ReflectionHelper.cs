using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudioAvaloniaDemo.Utils
{
    static class ReflectionHelper
    {
        public static IEnumerable<T> CreateAllInstancesOf<T>()
        {
            return typeof (ReflectionHelper).Assembly.GetTypes()
                .Where(t => typeof (T).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && t.IsClass)
                .Select(t => (T) Activator.CreateInstance(t));
        }
    }
}
