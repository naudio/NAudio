using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using NAudioWpfDemo.DrumMachineDemo;

namespace NAudioWpfDemo.EnumMediaFoundationTransforms
{
    [Export(typeof(IModule))]
    class EnumMftModule : ModuleBase
    {
        protected override UserControl CreateViewAndViewModel()
        {
            return new EnumMftView() { DataContext = new EnumMftViewModel() };
        }

        public override string Name
        {
            get { return "Enumerate MFTs"; }
        }

    }
}
