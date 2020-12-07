using System;
using System.Reflection;

namespace NAudio.Utils
{
    /// <summary>
    /// Helper to get descriptions
    /// </summary>
    public static class FieldDescriptionHelper
    {
        /// <summary>
        /// Describes the Guid  by looking for a FieldDescription attribute on the specified class
        /// </summary>
        public static string Describe(Type t, Guid guid)
        {
            // when we go to .NET 3.5, use LINQ for this
            foreach (var f in t
                .GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.IsPublic && f.IsStatic && f.FieldType == typeof (Guid) && (Guid) f.GetValue(null) == guid)
                {
                    foreach (var a in f.GetCustomAttributes(false))
                    {
                        var d = a as FieldDescriptionAttribute;
                        if (d != null)
                        {
                            return d.Description;
                        }
                    }
                    // no attribute, return the name
                    return f.Name;
                }
            }
            return guid.ToString();
        }
    }
}
