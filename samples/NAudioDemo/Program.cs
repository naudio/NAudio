using System;
using System.Windows.Forms;

namespace NAudioDemo;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Application.ThreadException += (_, e) => ShowError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowError(e.ExceptionObject as Exception);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var mainForm = new MainForm();
        Application.Run(mainForm);
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex?.ToString() ?? "Unknown error", "NAudioDemo",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
