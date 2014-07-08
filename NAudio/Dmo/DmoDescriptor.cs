using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    /// <summary>
    /// Contains the name and CLSID of a DirectX Media Object
    /// </summary>
    public class DmoDescriptor
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Clsid
        /// </summary>
        public Guid Clsid { get; private set; }

        /// <summary>
        /// Initializes a new instance of DmoDescriptor
        /// </summary>
        public DmoDescriptor(string name, Guid clsid)
        {
            this.Name = name;
            this.Clsid = clsid;
        }
    }
}
