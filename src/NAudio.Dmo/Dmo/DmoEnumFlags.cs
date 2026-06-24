using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// Defines flags for enumerating DMOs.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: DMO_ENUM_FLAGS (dmoreg.h).
    /// See https://learn.microsoft.com/windows/win32/api/dmoreg/ne-dmoreg-dmo_enum_flags
    /// </remarks>
    [Flags]
    enum DmoEnumFlags
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None,
        /// <summary>
        /// Include keyed DMOs in the enumeration.
        /// </summary>
        /// <remarks>DMO_ENUMF_INCLUDE_KEYED</remarks>
        IncludeKeyed = 0x00000001
    }
}
