using System;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Centralised catalogue of well-known VST 3 interface identifiers.
/// </summary>
/// <remarks>
/// <para>
/// These IIDs are derived from the <c>DECLARE_CLASS_IID</c> macros in the VST 3 SDK
/// (<a href="https://github.com/steinbergmedia/vst3_pluginterfaces">vst3_pluginterfaces</a>).
/// The header files use a <c>COM_COMPATIBLE</c> byte order on Windows that maps directly onto
/// the Microsoft GUID format — so the four <c>uint32</c> arguments to <c>DECLARE_CLASS_IID</c>
/// (l1, l2, l3, l4) translate to GUID Data1, (Data2 hi || Data3 hi), Data4[0..3], Data4[4..7].
/// </para>
/// <para>
/// On macOS and Linux the SDK uses a different byte order. Cross-platform support is deferred
/// to Phase 9; until then these constants assume Windows COM byte order, which matches every
/// plug-in DLL compiled for Windows.
/// </para>
/// </remarks>
internal static class Vst3StandardInterfaceIds
{
    // base/funknown.h
    public static readonly Guid FUnknown = new("00000000-0000-0000-C000-000000000046");

    // base/ipluginbase.h
    public static readonly Guid IPluginBase = new("22888DDB-156E-45AE-8358-B34808190625");
    public static readonly Guid IPluginFactory = new("7A4D811C-5211-4A1F-AED9-D2EE0B43BF9F");
    public static readonly Guid IPluginFactory2 = new("0007B650-F24B-4C0B-A464-EDB9F00B2ABB");
    public static readonly Guid IPluginFactory3 = new("4555A2AB-C123-4E57-9B12-291036878931");

    // base/ibstream.h
    public static readonly Guid IBStream = new("C3BF6EA2-3099-4752-9B6B-F9901EE33E9B");
    public static readonly Guid ISizeableStream = new("04F9549E-E02F-4E6E-87E8-6A8747F4E17F");

    // vst/ivstcomponent.h
    public static readonly Guid IComponent = new("E831FF31-F2D5-4301-928E-BBEE25697802");
    public static readonly Guid IConnectionPoint = new("70A4156F-6E6E-4026-9891-48BFAA60D8D1");

    // vst/ivstaudioprocessor.h
    public static readonly Guid IAudioProcessor = new("42043F99-B7DA-453C-A569-E79D9AAEC33D");
    public static readonly Guid IProcessContextRequirements = new("2A654303-EF76-4E3D-95B5-FE83730EF6D0");

    // vst/ivsteditcontroller.h
    public static readonly Guid IEditController = new("DCD7BBE3-7742-448D-A874-AACC979C759E");
    public static readonly Guid IComponentHandler = new("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6");
    public static readonly Guid IComponentHandler2 = new("F040B4B3-A360-45EC-ABCD-C045B4D5A2CC");
    public static readonly Guid IMidiMapping = new("DF0FF9F7-49B7-4669-B63A-B7327ADBF5E5");

    // vst/ivstunits.h
    public static readonly Guid IUnitInfo = new("3D4BD6B5-913A-4FD2-A886-E768A5EB92C1");

    // vst/ivsthostapplication.h
    public static readonly Guid IHostApplication = new("58E595CC-DB2D-4969-8B6A-AF8C36A664E5");

    // vst/ivstmessage.h
    public static readonly Guid IMessage = new("936F033B-C6C0-47DB-BB08-82F813C1E613");
    public static readonly Guid IAttributeList = new("1E5F0AEB-CC7F-4533-A254-401138AD5EE4");

    // vst/ivstattributes.h
    public static readonly Guid IStreamAttributes = new("D6CE2FFC-EFAF-4B8C-9E74-F1BB12DA44B4");

    // vst/ivstpluginterfacesupport.h
    public static readonly Guid IPlugInterfaceSupport = new("4FB58B9E-9EAA-4E0F-AB36-1C1CCCB56FEA");

    // vst/ivstparameterchanges.h
    public static readonly Guid IParameterChanges = new("A4779663-0BB6-4A56-B443-84A8466FEB9D");
    public static readonly Guid IParamValueQueue = new("01263A18-ED07-4F6F-98C9-D3564686F9BA");

    // vst/ivstevents.h
    public static readonly Guid IEventList = new("3A2C4214-3463-49FE-B2C4-F397B9695A44");

    // gui/iplugview.h
    public static readonly Guid IPlugView = new("5BC32507-D060-49EA-A615-1B522B755B29");
    public static readonly Guid IPlugFrame = new("367FAF01-AFA9-4693-8D4D-A2A0ED0882A3");
    public static readonly Guid IPlugViewContentScaleSupport = new("65ED9690-8AC4-4525-8AAD-EF7A72EA703F");
}
