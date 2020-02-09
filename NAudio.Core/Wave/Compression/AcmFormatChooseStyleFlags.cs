using System;

namespace NAudio.Wave.Compression
{
    [Flags]
    enum AcmFormatChooseStyleFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// ACMFORMATCHOOSE_STYLEF_SHOWHELP
        /// </summary>
        ShowHelp = 0x00000004,
        /// <summary>
        /// ACMFORMATCHOOSE_STYLEF_ENABLEHOOK
        /// </summary>
        EnableHook = 0x00000008,
        /// <summary>
        /// ACMFORMATCHOOSE_STYLEF_ENABLETEMPLATE
        /// </summary>
        EnableTemplate = 0x00000010,
        /// <summary>
        /// ACMFORMATCHOOSE_STYLEF_ENABLETEMPLATEHANDLE
        /// </summary>
        EnableTemplateHandle = 0x00000020,
        /// <summary>
        /// ACMFORMATCHOOSE_STYLEF_INITTOWFXSTRUCT
        /// </summary>
        InitToWfxStruct = 0x00000040,
        /// <summary>
        /// ACMFORMATCHOOSE_STYLEF_CONTEXTHELP
        /// </summary>
        ContextHelp = 0x00000080
    }
}
