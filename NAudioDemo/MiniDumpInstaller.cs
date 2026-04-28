using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace NAudioDemo
{
    /// <summary>
    /// Wires up multiple last-chance handlers (vectored exception handler, SEH unhandled
    /// exception filter, AppDomain unhandled exception, ProcessExit) and writes a full
    /// minidump on each. The vectored handler is the only thing that fires for
    /// <c>__fastfail</c>-style process kills (e.g. GS guard / FAIL_FAST_FATAL_APP_EXIT)
    /// because they bypass the normal SEH dispatch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dump destination: <c>%LOCALAPPDATA%\NAudioDemo\dumps\NAudioDemo-{timestamp}-{tag}.dmp</c>.
    /// Each handler tags the file with its source so multiple writes don't collide and
    /// you can tell which handler fired first.
    /// </para>
    /// <para>
    /// Open the resulting <c>.dmp</c> in WinDbg (or <c>windbg -z file.dmp</c>):
    /// <c>!analyze -v</c> for the failure summary, then <c>~*kb</c> for all thread stacks.
    /// The faulting thread is usually the one whose top frame is in <c>RtlFailFast</c>
    /// or <c>__report_gsfailure</c>.
    /// </para>
    /// </remarks>
    internal static class MiniDumpInstaller
    {
        private static int written;
        private static string dumpDirectory;
        private static string logPath;
        private static readonly object logLock = new object();

        public static void Install()
        {
            // WinExe doesn't have a console by default. AttachConsole connects us to
            // the parent (PowerShell / cmd) console if there is one — Console.WriteLine
            // then flows there. Errors silently ignored: if no parent console, fall
            // back to the log file below.
            try { AttachConsole(unchecked((uint)-1)); } catch { }

            try
            {
                dumpDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NAudioDemo", "dumps");
                Directory.CreateDirectory(dumpDirectory);
                logPath = Path.Combine(dumpDirectory, "minidump-installer.log");
                Log("===== MiniDumpInstaller.Install() ===== "
                    + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                Log("Dump directory: " + dumpDirectory);
                Log("Process: " + Process.GetCurrentProcess().Id + " "
                    + Environment.ProcessPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MiniDumpInstaller] Could not create dump directory: " + ex);
            }

            // 1. Vectored exception handler — first dispatch chance, fires before SEH
            // unwinding and crucially BEFORE __fastfail terminates the process. We pass
            // EXCEPTION_CONTINUE_SEARCH (0) so the OS continues normal handling
            // afterwards (i.e. the process still dies, we just got our dump first).
            try
            {
                vectoredHandlerDelegate = VectoredHandler; // keep alive
                IntPtr h = AddVectoredExceptionHandler(1u, vectoredHandlerDelegate);
                Log("AddVectoredExceptionHandler returned " + h);
            }
            catch (Exception ex)
            {
                Log("AddVectoredExceptionHandler failed: " + ex);
            }

            // 2. SetUnhandledExceptionFilter — SEH unhandled-exception path. Doesn't fire
            // for __fastfail but does fire for AVs, stack overflow (sometimes), etc.
            try
            {
                unhandledFilterDelegate = SehUnhandledFilter; // keep alive
                IntPtr prev = SetUnhandledExceptionFilter(unhandledFilterDelegate);
                Log("SetUnhandledExceptionFilter prev=" + prev);
            }
            catch (Exception ex)
            {
                Log("SetUnhandledExceptionFilter failed: " + ex);
            }

            // 3. AppDomain.UnhandledException — managed unhandled exceptions. Won't fire
            // for SEH/__fastfail but useful for normal CLR exception escapes.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Log("AppDomain.UnhandledException fired: terminating=" + e.IsTerminating
                    + " ex=" + e.ExceptionObject);
                WriteDump("AppDomainUnhandled");
            };

            // 4. ProcessExit backstop — fires on most clean exits and some fault paths.
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                Log("ProcessExit fired.");
            };

            // 5. First-chance for diagnostic visibility — logs every CLR exception
            // raised. Useful to see whether the crash is preceded by any managed throw.
            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                Log("FirstChanceException: " + e.Exception.GetType().Name
                    + " - " + e.Exception.Message);
            };

            Log("Install() complete.");
        }

        private static void Log(string message)
        {
            string line = "[MiniDumpInstaller] " + message;
            Debug.WriteLine(line);
            try { Console.WriteLine(line); } catch { }
            if (string.IsNullOrEmpty(logPath)) return;
            try
            {
                lock (logLock)
                {
                    File.AppendAllText(logPath,
                        DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + Environment.NewLine);
                }
            }
            catch { }
        }

        // ---- Vectored exception handler ----

        private delegate int VectoredHandlerDelegate(IntPtr exceptionPointers);
        private static VectoredHandlerDelegate vectoredHandlerDelegate;

        // EXCEPTION_RECORD layout (sufficient for what we need):
        //   DWORD ExceptionCode      [+0]
        //   DWORD ExceptionFlags     [+4]
        //   PTR   ExceptionRecord    [+8]
        //   PTR   ExceptionAddress   [+8 + sizeof(PTR)]
        //   ...
        private const int EXCEPTION_CONTINUE_SEARCH = 0;
        private const int EXCEPTION_EXECUTE_HANDLER = 1;

        private const uint STATUS_STACK_BUFFER_OVERRUN = 0xC0000409; // covers __fastfail
        private const uint STATUS_HEAP_CORRUPTION = 0xC0000374;
        private const uint STATUS_ACCESS_VIOLATION = 0xC0000005;
        private const uint EXCEPTION_NONCONTINUABLE = 0x1;

        private static int VectoredHandler(IntPtr exceptionPointers)
        {
            try
            {
                if (exceptionPointers == IntPtr.Zero) return EXCEPTION_CONTINUE_SEARCH;
                var pExceptionRecord = Marshal.ReadIntPtr(exceptionPointers);
                if (pExceptionRecord == IntPtr.Zero) return EXCEPTION_CONTINUE_SEARCH;

                uint code = (uint)Marshal.ReadInt32(pExceptionRecord);

                // We only want to capture genuinely fatal events — not the "first chance"
                // exceptions that fly through every CLR program. The codes below are the
                // crash-class ones that __fastfail and friends produce.
                if (code != STATUS_STACK_BUFFER_OVERRUN &&
                    code != STATUS_HEAP_CORRUPTION &&
                    code != STATUS_ACCESS_VIOLATION)
                {
                    return EXCEPTION_CONTINUE_SEARCH;
                }

                // Filter out non-fatal AVs (the CLR raises some during JIT etc.).
                uint flags = (uint)Marshal.ReadInt32(pExceptionRecord, 4);
                if (code == STATUS_ACCESS_VIOLATION && (flags & EXCEPTION_NONCONTINUABLE) == 0)
                {
                    // First-chance AV that something else may handle. Skip it; we'll fire
                    // again if it's actually fatal (CLR re-raises with NONCONTINUABLE).
                    return EXCEPTION_CONTINUE_SEARCH;
                }

                Log($"VectoredHandler firing: code=0x{code:X8} flags=0x{flags:X8}");
                WriteDump($"Vectored-0x{code:X8}", exceptionPointers);
            }
            catch
            {
                // Never throw out of a vectored handler.
            }
            return EXCEPTION_CONTINUE_SEARCH;
        }

        // ---- SetUnhandledExceptionFilter ----

        private delegate int UnhandledExceptionFilterDelegate(IntPtr exceptionPointers);
        private static UnhandledExceptionFilterDelegate unhandledFilterDelegate;

        private static int SehUnhandledFilter(IntPtr exceptionPointers)
        {
            try
            {
                Log("SehUnhandledFilter firing.");
                WriteDump("SehUnhandled", exceptionPointers);
            }
            catch { }
            return EXCEPTION_CONTINUE_SEARCH;
        }

        // ---- Minidump writer ----

        private static void WriteDump(string tag, IntPtr exceptionPointers = default)
        {
            // Cap the number of dumps in case a handler fires repeatedly. The first one
            // is the most informative anyway.
            int n = Interlocked.Increment(ref written);
            if (n > 5) return;
            if (string.IsNullOrEmpty(dumpDirectory)) return;

            try
            {
                var path = Path.Combine(dumpDirectory,
                    $"NAudioDemo-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{tag}.dmp");

                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                MiniDumpExceptionInformation info = default;
                IntPtr pInfo = IntPtr.Zero;
                if (exceptionPointers != IntPtr.Zero)
                {
                    info.ThreadId = GetCurrentThreadId();
                    info.ExceptionPointers = exceptionPointers;
                    info.ClientPointers = false;
                }

                bool ok;
                unsafe
                {
                    if (exceptionPointers != IntPtr.Zero)
                    {
                        ok = MiniDumpWriteDump(
                            GetCurrentProcess(),
                            (uint)Process.GetCurrentProcess().Id,
                            stream.SafeFileHandle.DangerousGetHandle(),
                            MiniDumpType.MiniDumpWithFullMemory |
                                MiniDumpType.MiniDumpWithHandleData |
                                MiniDumpType.MiniDumpWithUnloadedModules |
                                MiniDumpType.MiniDumpWithThreadInfo,
                            &info,
                            IntPtr.Zero,
                            IntPtr.Zero);
                    }
                    else
                    {
                        ok = MiniDumpWriteDump(
                            GetCurrentProcess(),
                            (uint)Process.GetCurrentProcess().Id,
                            stream.SafeFileHandle.DangerousGetHandle(),
                            MiniDumpType.MiniDumpWithFullMemory |
                                MiniDumpType.MiniDumpWithHandleData |
                                MiniDumpType.MiniDumpWithUnloadedModules |
                                MiniDumpType.MiniDumpWithThreadInfo,
                            null,
                            IntPtr.Zero,
                            IntPtr.Zero);
                    }
                }

                Log($"Wrote dump ({tag}) -> {path} (ok={ok}, lastError={Marshal.GetLastWin32Error()})");
            }
            catch (Exception ex)
            {
                Log("Dump write failed: " + ex);
            }
        }

        // ---- P/Invoke declarations ----

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr AddVectoredExceptionHandler(uint first, VectoredHandlerDelegate handler);

        [DllImport("kernel32.dll")]
        private static extern IntPtr SetUnhandledExceptionFilter(UnhandledExceptionFilterDelegate filter);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("dbghelp.dll", SetLastError = true)]
        private static extern unsafe bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            IntPtr hFile,
            MiniDumpType dumpType,
            MiniDumpExceptionInformation* exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct MiniDumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            [MarshalAs(UnmanagedType.Bool)] public bool ClientPointers;
        }

        [Flags]
        private enum MiniDumpType : uint
        {
            MiniDumpNormal = 0x0,
            MiniDumpWithDataSegs = 0x1,
            MiniDumpWithFullMemory = 0x2,
            MiniDumpWithHandleData = 0x4,
            MiniDumpWithUnloadedModules = 0x20,
            MiniDumpWithThreadInfo = 0x1000,
        }
    }
}
