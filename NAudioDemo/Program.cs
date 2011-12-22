using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel.Composition.Hosting;
using Microsoft.ComponentModel.Composition.Hosting;

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
            var exportFactoryProvider = new ExportFactoryProvider(); // enable use of ExportFactory
            var container = new CompositionContainer(catalog, exportFactoryProvider);
            exportFactoryProvider.SourceProvider = container; // enable use of ExportFactory
            var mainForm = container.GetExportedValue<MainForm>();
            Application.Run(mainForm);
        }
    }
}