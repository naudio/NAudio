using System;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// PROPVARIANT type tags from propidl.h (the VT_* constants).
    /// Numeric values match the COM ABI, so this enum is interchangeable with the
    /// PROPVARIANT.vt field via cast. Defined locally to avoid System.Runtime.InteropServices.VarEnum,
    /// which is obsolete (SYSLIB0050) as part of the deprecation of built-in VARIANT marshalling.
    /// </summary>
    [Flags]
    public enum VarType : ushort
    {
        /// <summary>VT_EMPTY — no value.</summary>
        VT_EMPTY = 0,
        /// <summary>VT_NULL — SQL-style null.</summary>
        VT_NULL = 1,
        /// <summary>VT_I2 — signed 16-bit integer.</summary>
        VT_I2 = 2,
        /// <summary>VT_I4 — signed 32-bit integer.</summary>
        VT_I4 = 3,
        /// <summary>VT_R4 — 32-bit float.</summary>
        VT_R4 = 4,
        /// <summary>VT_R8 — 64-bit double.</summary>
        VT_R8 = 5,
        /// <summary>VT_BOOL — VARIANT_BOOL (-1 = true, 0 = false).</summary>
        VT_BOOL = 11,
        /// <summary>VT_I1 — signed 8-bit integer.</summary>
        VT_I1 = 16,
        /// <summary>VT_UI1 — unsigned 8-bit integer.</summary>
        VT_UI1 = 17,
        /// <summary>VT_UI2 — unsigned 16-bit integer.</summary>
        VT_UI2 = 18,
        /// <summary>VT_UI4 — unsigned 32-bit integer.</summary>
        VT_UI4 = 19,
        /// <summary>VT_I8 — signed 64-bit integer.</summary>
        VT_I8 = 20,
        /// <summary>VT_UI8 — unsigned 64-bit integer.</summary>
        VT_UI8 = 21,
        /// <summary>VT_INT — signed machine-word integer.</summary>
        VT_INT = 22,
        /// <summary>VT_UINT — unsigned machine-word integer.</summary>
        VT_UINT = 23,
        /// <summary>VT_LPSTR — pointer to a null-terminated ANSI string.</summary>
        VT_LPSTR = 30,
        /// <summary>VT_LPWSTR — pointer to a null-terminated UTF-16 string.</summary>
        VT_LPWSTR = 31,
        /// <summary>VT_FILETIME — 64-bit FILETIME.</summary>
        VT_FILETIME = 64,
        /// <summary>VT_BLOB — counted byte array (length + pointer).</summary>
        VT_BLOB = 65,
        /// <summary>VT_CLSID — pointer to a GUID.</summary>
        VT_CLSID = 72,
        /// <summary>VT_VECTOR — flag combined with another VT_* to indicate a counted array (CA*).</summary>
        VT_VECTOR = 0x1000,
    }
}
