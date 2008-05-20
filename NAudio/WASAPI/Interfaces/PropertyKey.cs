using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.WASAPI.Interfaces
{
    /// <summary>
    /// PROPERTYKEY is defined in wtypes.h
    /// </summary>
    class PropertyKey
    {
        Guid formatId;
        int propertyId;
    }
}
