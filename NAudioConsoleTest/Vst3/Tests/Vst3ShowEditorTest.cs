using System.Runtime.InteropServices;
using NAudio.Vst3;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Phase 6 smoke harness: instantiates a VST 3 plug-in, creates its editor view, and embeds it in
/// a bare Win32 window on a dedicated STA thread, then pumps messages until the window is closed.
/// This deliberately uses no UI framework — it proves the framework-agnostic
/// <see cref="Vst3PluginView.AttachTo(IntPtr, float)"/> path against a plain <c>HWND</c> before the
/// WPF demo layers a framework on top.
/// </summary>
/// <remarks>
/// Interactive only: there's nothing to assert headlessly — verification is "the editor renders,
/// it tracks resize requests, and the window closes cleanly". No audio is wired up here; that's
/// the WPF demo's job. The plug-in is created, its view attached, but <c>process()</c> is never
/// called, so knobs won't make sound — they should still move and redraw.
/// </remarks>
sealed class Vst3ShowEditorTest : IConsoleTest
{
    public string Id => "Vst3.ShowEditor";
    public string Description => "Embed a VST 3 plug-in editor in a bare Win32 window (no audio)";
    public MenuPath? MenuLocation => new("VST 3", "Show a VST 3 plug-in editor", Group: "UI", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "plug-in name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");

        var matches = Vst3PluginScanner.EnumerateInstalled()
            .Where(m => m.Name.Contains(pluginQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
            return TestResult.Fail($"No installed plug-in matches '{pluginQuery}'");
        if (matches.Count > 1)
            return TestResult.Fail($"Multiple matches for '{pluginQuery}': {string.Join(", ", matches.Take(10).Select(m => m.Name))}");

        var moduleInfo = matches[0];
        Console.WriteLine($"Plug-in : {moduleInfo.Name}");
        Console.WriteLine($"  path  : {moduleInfo.Path}");

        // The whole plug-in + view lifecycle must live on one STA thread that owns the message
        // pump — that's the VST 3 UI-thread contract. We run it on a worker so the console thread
        // (which may not be STA, and is busy with the menu) stays out of it, then join.
        string? error = null;
        Vst3ViewSize finalSize = default;
        var editorShown = false;

        var uiThread = new Thread(() =>
        {
            try
            {
                using var module = Vst3Module.Load(moduleInfo.Path);
                var audioClass = module.GetClasses().FirstOrDefault(c => c.Category == "Audio Module Class");
                if (audioClass is null)
                {
                    error = $"{moduleInfo.Name} has no Audio Module Class entry";
                    return;
                }

                using var plugin = module.CreatePlugin(audioClass, 44100, 512);
                using var view = plugin.CreateView();
                if (view is null)
                {
                    error = $"{moduleInfo.Name} ({audioClass.Name}) provides no editor view (createView returned null)";
                    return;
                }

                EditorWindow.Show(view, moduleInfo.Name, out finalSize);
                editorShown = true;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}: {ex.Message}";
            }
        });
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();
        uiThread.Join();

        if (error is not null)
            return TestResult.Fail(error);
        if (!editorShown)
            return TestResult.Fail("Editor window did not open.");

        return TestResult.Pass(
            $"Editor for {moduleInfo.Name} shown and closed cleanly ({finalSize.Width}x{finalSize.Height})",
            new Dictionary<string, string>
            {
                ["plugin"] = moduleInfo.Name,
                ["editorWidth"] = finalSize.Width.ToString(),
                ["editorHeight"] = finalSize.Height.ToString(),
            });
    }

    /// <summary>
    /// A minimal Win32 host window. Owns the window class, the message loop, and the
    /// plug-in→host resize bridge. All members run on the calling (STA) thread.
    /// </summary>
    private static class EditorWindow
    {
        public static void Show(Vst3PluginView view, string title, out Vst3ViewSize finalSize)
        {
            var hwnd = IntPtr.Zero;
            // Ignore WM_SIZE until the editor is attached and initially sized.
            var ready = false;
            // Reentrancy guard: a resize we drive ourselves (responding to a plug-in resizeView)
            // fires WM_SIZE synchronously inside SetWindowPos. We must not echo that back to the
            // plug-in as a fresh host-window resize, or the two chase each other.
            var resizingFromPlugin = false;

            // Resizes the host window so its *client* area exactly fits the editor — used when the
            // plug-in initiates a resize (IPlugFrame::resizeView, e.g. its own drag handle or a
            // zoom menu) so the window tracks the editor.
            void ResizeClientArea(int clientWidth, int clientHeight)
            {
                var style = (uint)GetWindowLongPtr(hwnd, GWL_STYLE);
                var rect = new RECT { Left = 0, Top = 0, Right = clientWidth, Bottom = clientHeight };
                AdjustWindowRect(ref rect, style, false);
                resizingFromPlugin = true;
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0,
                    rect.Right - rect.Left, rect.Bottom - rect.Top,
                    SWP_NOMOVE | SWP_NOZORDER);
                resizingFromPlugin = false;
            }

            // Keep the WndProc delegate rooted for the lifetime of the window — otherwise the GC
            // can collect it while Win32 still holds the function pointer.
            //
            // Resize model (matches REAPER): the host window and editor track each other in both
            // directions. The user resizing the host window is reported to the plug-in via
            // IPlugView::onSize (WM_SIZE below); the plug-in then zooms, reflows, or ignores it as
            // it sees fit. Plug-in-initiated resizes (resizeView) drive the window the other way via
            // ResizeClientArea.
            WndProcDelegate wndProc = (hWnd, msg, wParam, lParam) =>
            {
                switch (msg)
                {
                    case WM_SIZE:
                        // The user (or OS) resized the host window — report the new client area to
                        // the plug-in. lParam packs client width/height as two 16-bit words.
                        if (ready && !resizingFromPlugin)
                        {
                            var w = (int)(lParam.ToInt64() & 0xFFFF);
                            var h = (int)((lParam.ToInt64() >> 16) & 0xFFFF);
                            if (w > 0 && h > 0) view.SetSize(w, h);
                        }
                        return IntPtr.Zero;
                    case WM_CLOSE:
                        // Detach the editor while its parent HWND is still valid (SDK contract),
                        // then tear the window down.
                        view.Detach();
                        DestroyWindow(hWnd);
                        return IntPtr.Zero;
                    case WM_DESTROY:
                        PostQuitMessage(0);
                        return IntPtr.Zero;
                    default:
                        return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            };

            var className = "NAudioVst3Host_" + Guid.NewGuid().ToString("N");
            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
                hInstance = GetModuleHandle(null),
                hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
                // Paint the chrome around the editor with the standard button-face grey, so the
                // area exposed when the host window is larger than the editor isn't left unpainted.
                hbrBackground = (IntPtr)(COLOR_BTNFACE + 1),
                lpszClassName = className,
            };
            if (RegisterClassEx(ref wndClass) == 0)
                throw new InvalidOperationException($"RegisterClassEx failed (Win32 error {Marshal.GetLastWin32Error()}).");

            hwnd = CreateWindowEx(
                0, className, $"{title} — VST 3 editor", WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT, CW_USEDEFAULT, 400, 300,
                IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException($"CreateWindowEx failed (Win32 error {Marshal.GetLastWin32Error()}).");

            try
            {
                var dpi = GetDpiForWindow(hwnd);
                var scale = dpi > 0 ? dpi / 96.0f : 1.0f;
                Console.WriteLine($"  DPI   : {dpi} ({scale:0.##}x scale)");

                // Resize the host window whenever the plug-in asks (e.g. it computes its natural
                // size during attach). Runs on this STA thread inside message dispatch.
                view.Resized += (_, size) => ResizeClientArea(size.Width, size.Height);

                view.AttachTo(hwnd, scale);

                finalSize = view.GetSize();
                if (finalSize.Width > 0 && finalSize.Height > 0)
                    ResizeClientArea(finalSize.Width, finalSize.Height);

                // Informational only — the host window stays resizable regardless. CanResize()
                // tells us whether the *editor* offers its own resize affordance (drag handle).
                Console.WriteLine($"  editor: {finalSize.Width}x{finalSize.Height}, editor self-resizable={view.CanResize()}");
                Console.WriteLine("  >> Close the editor window to continue.");

                // From here on, honour user-driven host-window resizes (report them to the plug-in).
                ready = true;

                ShowWindow(hwnd, SW_SHOW);
                UpdateWindow(hwnd);

                // Standard Win32 message loop. Exits when WM_DESTROY posts WM_QUIT. The view is
                // detached in the WM_CLOSE handler, before the HWND is destroyed.
                while (GetMessage(out var m, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref m);
                    DispatchMessage(ref m);
                }
            }
            finally
            {
                UnregisterClass(className, wndClass.hInstance);
                GC.KeepAlive(wndProc);
            }
        }

        // ---- Win32 interop ----

        private const uint WM_SIZE = 0x0005;
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_DESTROY = 0x0002;
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const int SW_SHOW = 5;
        private const int GWL_STYLE = -16;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const int COLOR_BTNFACE = 15;
        private static readonly IntPtr IDC_ARROW = 32512;

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
            [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool UnregisterClass(string className, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            uint exStyle, string className, string windowName, uint style,
            int x, int y, int width, int height,
            IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int exitCode);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG msg, IntPtr hWnd, uint filterMin, uint filterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG msg);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern bool AdjustWindowRect(ref RECT rect, uint style, bool menu);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr cursorName);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? moduleName);
    }
}
