using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel.Composition.Hosting;

namespace NAudioDemo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        //[MTAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var catalog = new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            var mainForm = container.GetExportedValue<MainForm>();
            Application.Run(mainForm);
        }
    }
}