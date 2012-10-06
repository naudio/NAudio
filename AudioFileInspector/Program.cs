using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace AudioFileInspector
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var catalog = new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly());            
            var container = new CompositionContainer(catalog);
            var inspectors = container.GetExportedValues<IAudioFileInspector>();
            
            if (args.Length > 0)
            {
                if (args[0] == "-install")
                {
                    try
                    {
                        OptionsForm.Associate(inspectors);
                        Console.WriteLine("Created {0} file associations", inspectors.Count()); 
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to create file associations");
                        Console.WriteLine(e.ToString());
                        return -1;
                    }

                    return 0;
                }
                else if (args[0] == "-uninstall")
                {
                    try
                    {
                        OptionsForm.Disassociate(inspectors);
                        Console.WriteLine("Removed {0} file associations", inspectors.Count());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to remove file associations");
                        Console.WriteLine(e.ToString());
                        return -1;
                    }
                    return 0;
                }
            }
            var mainForm = container.GetExportedValue<AudioFileInspectorForm>();
            mainForm.CommandLineArguments = args;
            Application.Run(mainForm);
            return 0;
        }
    }
}