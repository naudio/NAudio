using System;
using System.Windows;
using NAudio.Vst3;
using NAudioWpfDemo.Vst3HostDemo;

namespace NAudioWpfDemo.Vst3Shared
{
    /// <summary>
    /// Pop-out window that hosts a single VST 3 plug-in's native editor via
    /// <see cref="Vst3EditorHost"/>. The window sizes itself to the editor's reported
    /// size and tracks plug-in-initiated resizes. Closing the window detaches the editor
    /// (which is mandatory before the plug-in is torn down).
    /// </summary>
    partial class Vst3EditorWindow : Window
    {
        private readonly Vst3EditorHost editorHost;

        public Vst3EditorWindow(string title, Vst3PluginView view)
        {
            InitializeComponent();
            Title = title;
            editorHost = new Vst3EditorHost(view);
            EditorHostSlot.Content = editorHost;
            Closed += OnClosed;
        }

        public event EventHandler ClosedByUser;

        private void OnClosed(object sender, EventArgs e)
        {
            editorHost.Dispose();
            ClosedByUser?.Invoke(this, EventArgs.Empty);
        }
    }
}
