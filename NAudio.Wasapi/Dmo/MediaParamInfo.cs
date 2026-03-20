using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// MP_PARAMINFO
    /// </summary>
    struct MediaParamInfo
    {
#pragma warning disable 0649
        public MediaParamType mpType;
        public MediaParamCurveType mopCaps;
        public float mpdMinValue; // MP_DATA is a float
        public float mpdMaxValue;
        public float mpdNeutralValue;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szUnitText;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szLabel;
#pragma warning restore 0649
    }

    /// <summary>
    /// MP_TYPE
    /// </summary>
    enum MediaParamType 
    {
        /// <summary>
        /// MPT_INT
        /// </summary>
        Int,
        /// <summary>
        /// MPT_FLOAT
        /// </summary>
        Float,
        /// <summary>
        /// MPT_BOOL
        /// </summary>
        Bool,
        /// <summary>
        /// MPT_ENUM
        /// </summary>
        Enum,
        /// <summary>
        /// MPT_MAX
        /// </summary>
        Max,
    }

    /// <summary>
    /// Defines the curve types for media parameter transitions.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: MP_CURVE_TYPE (medparam.h).
    /// See https://learn.microsoft.com/windows/win32/api/medparam/ne-medparam-mp_curve_type
    /// </remarks>
    [Flags]
    internal enum MediaParamCurveType
    {
        /// <summary>
        /// Instantaneous jump to the new value.
        /// </summary>
        /// <remarks>MP_CURVE_JUMP</remarks>
        Jump = 0x1,
        /// <summary>
        /// Linear interpolation to the new value.
        /// </summary>
        /// <remarks>MP_CURVE_LINEAR</remarks>
        Linear = 0x2,
        /// <summary>
        /// Square curve interpolation to the new value.
        /// </summary>
        /// <remarks>MP_CURVE_SQUARE</remarks>
        Square = 0x4,
        /// <summary>
        /// Inverse square curve interpolation to the new value.
        /// </summary>
        /// <remarks>MP_CURVE_INVSQUARE</remarks>
        InverseSquare = 0x8,
        /// <summary>
        /// Sine curve interpolation to the new value.
        /// </summary>
        /// <remarks>MP_CURVE_SINE</remarks>
        Sine = 0x10
    }

}
