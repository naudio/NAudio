using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3;

/// <summary>
/// A loaded VST 3® plug-in module — the host's handle to a single <c>.vst3</c> file or bundle.
/// </summary>
/// <remarks>
/// <para>
/// Phase 0 surface — only enough to walk the plug-in factory and enumerate its classes. Plug-in
/// instantiation, audio processing, UI hosting, MIDI events, and parameter access land in
/// later phases (see <c>Docs/Architecture/Vst3Hosting.md</c>).
/// </para>
/// <para>
/// VST is a registered trademark of Steinberg Media Technologies GmbH.
/// </para>
/// </remarks>
public sealed class Vst3Module : IDisposable
{
    private const string GetPluginFactoryExport = "GetPluginFactory";
    private const string InitDllExport = "InitDll";
    private const string ExitDllExport = "ExitDll";

    private IntPtr _moduleHandle;
    private IPluginFactory? _factory;
    private bool _initDllCalled;

    private Vst3Module(IntPtr moduleHandle, IPluginFactory factory, bool initDllCalled)
    {
        _moduleHandle = moduleHandle;
        _factory = factory;
        _initDllCalled = initDllCalled;
    }

    /// <summary>The resolved path of the loaded native module.</summary>
    public string Path { get; private init; } = string.Empty;

    /// <summary>
    /// Loads the <c>.vst3</c> at <paramref name="path"/>. Accepts either a bare DLL or a VST 3
    /// folder bundle — for the bundle form, the inner
    /// <c>Contents/x86_64-win/&lt;Plugin&gt;.vst3</c> binary is resolved automatically.
    /// </summary>
    public static Vst3Module Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var resolved = ResolveBundleBinary(path);
        var moduleHandle = NativeLibrary.Load(resolved);
        var initDllCalled = false;
        IPluginFactory? factory = null;
        var factoryPtr = IntPtr.Zero;

        try
        {
            if (NativeLibrary.TryGetExport(moduleHandle, InitDllExport, out var initDllPtr))
            {
                var initDll = Marshal.GetDelegateForFunctionPointer<InitDllDelegate>(initDllPtr);
                initDll();
                initDllCalled = true;
            }

            var getFactoryPtr = NativeLibrary.GetExport(moduleHandle, GetPluginFactoryExport);
            var getFactory = Marshal.GetDelegateForFunctionPointer<GetPluginFactoryDelegate>(getFactoryPtr);
            factoryPtr = getFactory();
            if (factoryPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"GetPluginFactory returned null for '{resolved}'.");
            }

            factory = (IPluginFactory)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
                factoryPtr, CreateObjectFlags.UniqueInstance);

            return new Vst3Module(moduleHandle, factory, initDllCalled)
            {
                Path = resolved,
            };
        }
        catch
        {
            // factoryPtr is released by the finally below (which runs on this path too); releasing it
            // here as well would be a double-release. Just unwind the native module load.
            if (initDllCalled && NativeLibrary.TryGetExport(moduleHandle, ExitDllExport, out var exitDllPtr))
            {
                var exitDll = Marshal.GetDelegateForFunctionPointer<ExitDllDelegate>(exitDllPtr);
                exitDll();
            }
            NativeLibrary.Free(moduleHandle);
            throw;
        }
        finally
        {
            // Release the ref returned by GetPluginFactory exactly once. On the success path the
            // ComWrappers RCW has taken its own ref via QueryInterface, so this drops the factory
            // function's ref; on the failure path nothing else owns it. Runs on both the return and
            // the exception path — which is why the catch must not release it too.
            if (factoryPtr != IntPtr.Zero)
            {
                Marshal.Release(factoryPtr);
            }
        }
    }

    /// <summary>Reads the factory's vendor / url / email metadata.</summary>
    public Vst3FactoryInfo GetFactoryInfo()
    {
        var factory = _factory ?? throw new ObjectDisposedException(nameof(Vst3Module));
        var hr = factory.GetFactoryInfo(out PFactoryInfo info);
        if (hr != TResultCodes.Ok)
        {
            throw new InvalidOperationException(
                $"IPluginFactory::getFactoryInfo failed (HRESULT 0x{hr:X8}).");
        }

        unsafe
        {
            return new Vst3FactoryInfo(
                ReadUtf8(info.Vendor, PFactoryInfo.NameSize),
                ReadUtf8(info.Url, PFactoryInfo.UrlSize),
                ReadUtf8(info.Email, PFactoryInfo.EmailSize),
                info.Flags);
        }
    }

    /// <summary>
    /// Enumerates every class exported by the factory. Uses <c>IPluginFactory3</c> when
    /// available (unicode strings, version metadata), falling back to v2 and finally v1.
    /// </summary>
    public IReadOnlyList<Vst3ClassInfo> GetClasses()
    {
        var factory = _factory ?? throw new ObjectDisposedException(nameof(Vst3Module));
        var count = factory.CountClasses();
        if (count <= 0)
        {
            return Array.Empty<Vst3ClassInfo>();
        }

        var factory3 = factory as IPluginFactory3;
        var factory2 = factory as IPluginFactory2;
        var result = new List<Vst3ClassInfo>(count);

        for (var i = 0; i < count; i++)
        {
            if (factory3 is not null
                && factory3.GetClassInfoUnicode(i, out PClassInfoW infoW) == TResultCodes.Ok)
            {
                result.Add(FromUnicodeInfo(infoW));
                continue;
            }
            if (factory2 is not null
                && factory2.GetClassInfo2(i, out PClassInfo2 info2) == TResultCodes.Ok)
            {
                result.Add(FromV2Info(info2));
                continue;
            }
            if (factory.GetClassInfo(i, out PClassInfo info) == TResultCodes.Ok)
            {
                result.Add(FromV1Info(info));
            }
        }

        return result;
    }

    /// <summary>
    /// Instantiates a plug-in for the given class. The class is normally one of the
    /// <c>"Audio Module Class"</c> entries returned by <see cref="GetClasses"/>.
    /// </summary>
    /// <param name="classInfo">The class descriptor (typically from <see cref="GetClasses"/>).</param>
    /// <param name="sampleRate">Sample rate to configure on the plug-in (e.g. 44100, 48000).</param>
    /// <param name="maxBlockSize">Maximum block size in samples the host will request per process call.</param>
    /// <remarks>
    /// Phase 2: only stereo-in / stereo-out audio effects are supported. The returned plug-in
    /// instance must be disposed before the parent <see cref="Vst3Module"/>.
    /// </remarks>
    public Vst3Plugin CreatePlugin(Vst3ClassInfo classInfo, int sampleRate, int maxBlockSize)
    {
        var factory = _factory ?? throw new ObjectDisposedException(nameof(Vst3Module));
        ArgumentNullException.ThrowIfNull(classInfo);
        return new Vst3Plugin(factory, classInfo, sampleRate, maxBlockSize);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_factory is not null)
        {
            // Release the QueryInterface AddRef done by GetOrCreateObjectForComInstance.
            ((ComObject)(object)_factory).FinalRelease();
            _factory = null;
        }
        if (_moduleHandle != IntPtr.Zero)
        {
            if (_initDllCalled
                && NativeLibrary.TryGetExport(_moduleHandle, ExitDllExport, out var exitDllPtr))
            {
                var exitDll = Marshal.GetDelegateForFunctionPointer<ExitDllDelegate>(exitDllPtr);
                exitDll();
                _initDllCalled = false;
            }
            NativeLibrary.Free(_moduleHandle);
            _moduleHandle = IntPtr.Zero;
        }
    }

    private static string ResolveBundleBinary(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }
        if (Directory.Exists(path))
        {
            var bundleName = System.IO.Path.GetFileNameWithoutExtension(path);
            var contents = System.IO.Path.Combine(path, "Contents", "x86_64-win", bundleName + ".vst3");
            if (File.Exists(contents))
            {
                return contents;
            }
        }
        throw new FileNotFoundException(
            $"Could not resolve a VST 3 module binary from '{path}'.", path);
    }

    private static unsafe string ReadUtf8(byte* buffer, int maxBytes)
    {
        var span = new ReadOnlySpan<byte>(buffer, maxBytes);
        var nul = span.IndexOf((byte)0);
        if (nul >= 0)
        {
            span = span[..nul];
        }
        return Encoding.UTF8.GetString(span);
    }

    private static unsafe string ReadUtf8Fixed(byte* buffer, int maxBytes)
        => ReadUtf8(buffer, maxBytes);

    private static unsafe string ReadUtf16(char* buffer, int maxChars)
    {
        var span = new ReadOnlySpan<char>(buffer, maxChars);
        var nul = span.IndexOf('\0');
        if (nul >= 0)
        {
            span = span[..nul];
        }
        return new string(span);
    }

    private static unsafe Vst3ClassInfo FromV1Info(PClassInfo info)
        => new(
            Vst3Tuid.Format(info.Cid),
            ReadUtf8Fixed(info.Category, PClassInfo.CategorySize),
            ReadUtf8Fixed(info.Name, PClassInfo.NameSize),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

    private static unsafe Vst3ClassInfo FromV2Info(PClassInfo2 info)
        => new(
            Vst3Tuid.Format(info.Cid),
            ReadUtf8Fixed(info.Category, PClassInfo.CategorySize),
            ReadUtf8Fixed(info.Name, PClassInfo.NameSize),
            ReadUtf8Fixed(info.Vendor, PClassInfo2.VendorSize),
            ReadUtf8Fixed(info.Version, PClassInfo2.VersionSize),
            ReadUtf8Fixed(info.SdkVersion, PClassInfo2.VersionSize),
            ReadUtf8Fixed(info.SubCategories, PClassInfo2.SubCategoriesSize));

    private static unsafe Vst3ClassInfo FromUnicodeInfo(PClassInfoW info)
        => new(
            Vst3Tuid.Format(info.Cid),
            ReadUtf8Fixed(info.Category, PClassInfo.CategorySize),
            ReadUtf16(info.Name, PClassInfo.NameSize),
            ReadUtf16(info.Vendor, PClassInfo2.VendorSize),
            ReadUtf16(info.Version, PClassInfo2.VersionSize),
            ReadUtf16(info.SdkVersion, PClassInfo2.VersionSize),
            ReadUtf8Fixed(info.SubCategories, PClassInfo2.SubCategoriesSize));

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr GetPluginFactoryDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate byte InitDllDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate byte ExitDllDelegate();
}
