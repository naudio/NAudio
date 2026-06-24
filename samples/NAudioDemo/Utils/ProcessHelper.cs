using System.Diagnostics;

namespace NAudioDemo.Utils
{
    static class ProcessHelper
    {
        public static void ShellExecute(string file)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(file)
            {
                UseShellExecute = true
            };
            process.Start();
        }
    }
}
