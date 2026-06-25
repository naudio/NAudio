using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using NAudio.Vst3;

namespace NAudioWpfDemo.Vst3HostDemo;

/// <summary>
/// A WPF <see cref="HwndHost"/> that embeds a VST 3 plug-in editor. It creates a child container
/// window, attaches the <see cref="Vst3PluginView"/> to it (pushing the host DPI scale), and
/// resizes itself to track plug-in-initiated resizes (the editor's own drag handle / zoom menu).
/// </summary>
/// <remarks>
/// All members run on the WPF UI (STA) thread — the same thread the <see cref="Vst3PluginView"/>
/// was created on, satisfying the VST 3 UI-thread contract.
/// </remarks>
class Vst3EditorHost : HwndHost
{
    // Single registered class for every editor-host container window in this process. Using
    // a registered class rather than reusing the built-in "STATIC" is what makes NI Raum (and
    // probably other parent-snooping plug-ins) embed cleanly — STATIC has subtle differences
    // a few editors trip over, e.g. cbWndExtra/cbClsExtra defaults and the absence of CS_DBLCLKS.
    private const string ContainerClassName = "NAudio.Vst3.EditorHostContainer";
    private static readonly object classRegLock = new();
    private static bool classRegistered;
    private static WndProcDelegate containerWndProc; // rooted so the JIT can't GC the delegate while Win32 holds the fn ptr

    private readonly Vst3PluginView view;
    private IntPtr container;
    private float dpiScale = 1.0f;

    public Vst3EditorHost(Vst3PluginView view)
    {
        this.view = view;
        view.Resized += OnPluginResized;
    }

    private static void EnsureContainerClassRegistered()
    {
        if (classRegistered) return;
        lock (classRegLock)
        {
            if (classRegistered) return;
            containerWndProc = (h, m, w, l) => DefWindowProc(h, m, w, l);
            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(containerWndProc),
                hInstance = GetModuleHandle(null),
                hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
                hbrBackground = (IntPtr)(COLOR_BTNFACE + 1),
                lpszClassName = ContainerClassName,
            };
            if (RegisterClassEx(ref wndClass) == 0)
            {
                var err = Marshal.GetLastWin32Error();
                // ERROR_CLASS_ALREADY_EXISTS = 1410: harmless, another HwndHost in the same
                // process already registered our class — just mark it ready.
                if (err != 1410)
                    throw new InvalidOperationException($"RegisterClassEx failed (Win32 error {err}).");
            }
            classRegistered = true;
        }
    }

    private void OnPluginResized(object sender, Vst3ViewSize size)
    {
        if (!CheckAccess())
        {
            Dispatcher.Invoke(() => ApplySize(size));
            return;
        }
        ApplySize(size);
    }

    private void ApplySize(Vst3ViewSize size)
    {
        // GetSize reports physical pixels; WPF layout works in DIPs, so divide by the scale.
        // Setting Width/Height drives the HwndHost (and the container it owns) to this size.
        Width = dpiScale > 0 ? size.Width / dpiScale : size.Width;
        Height = dpiScale > 0 ? size.Height / dpiScale : size.Height;
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var dpi = GetDpiForWindow(hwndParent.Handle);
        dpiScale = dpi > 0 ? dpi / 96.0f : 1.0f;

        EnsureContainerClassRegistered();
        container = CreateWindowEx(
            0, ContainerClassName, null,
            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
            0, 0, 100, 100,
            hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (container == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to create the editor container window (Win32 error {Marshal.GetLastWin32Error()}).");
        }

        view.AttachTo(container, dpiScale);
        ApplySize(view.GetSize());
        return new HandleRef(this, container);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        // Detach the editor while its parent container is still valid, then destroy it.
        view.Detach();
        if (container != IntPtr.Zero)
        {
            DestroyWindow(container);
            container = IntPtr.Zero;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            view.Resized -= OnPluginResized;
        }
        base.Dispose(disposing);
    }

    private const uint WS_CHILD = 0x40000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_CLIPCHILDREN = 0x02000000;
    private const int COLOR_BTNFACE = 15;
    private static readonly IntPtr IDC_ARROW = (IntPtr)32512;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint exStyle, string className, string windowName, uint style,
        int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
