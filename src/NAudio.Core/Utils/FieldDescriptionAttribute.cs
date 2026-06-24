using System;

namespace NAudio.Utils
{
    /// <summary>
    /// Allows us to add descriptions to interop members
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FieldDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Field description
        /// </summary>
        public FieldDescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Description;
        }
    }
}
