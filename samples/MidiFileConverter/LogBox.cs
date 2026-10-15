using System;
using System.Drawing;
using System.Windows.Forms;

namespace MarkHeath.MidiUtils;

/// <summary>
/// A RichTextBox that can be logged to from any thread
/// </summary>
internal class LogBox : RichTextBox
{
    public LogBox()
    {
        ReadOnly = true;
    }

    public void LogMessage(Color color, string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => LogMessage(color, message));
            return;
        }
        SelectionStart = TextLength;
        SelectionColor = color;
        AppendText(message);
        AppendText(Environment.NewLine);
    }

    public void ClearLog()
    {
        if (InvokeRequired)
        {
            Invoke(ClearLog);
            return;
        }
        Clear();
    }
}
