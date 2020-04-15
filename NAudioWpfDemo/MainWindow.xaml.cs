using System;
using System.Windows;

namespace NAudioWpfDemo
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var arch = Environment.Is64BitProcess ? "x64" : "x86";
            var framework = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
            this.Title = $"{this.Title} ({framework}) ({arch})";
        }
    }
}
