using System;
using System.Collections.Generic;
using System.Text;
using NAudio.FileFormats.Map;
using System.ComponentModel.Composition;

namespace AudioFileInspector
{
    [Export(typeof(IAudioFileInspector))]
    public class CakewalkMapInspector : IAudioFileInspector
    {
        #region IAudioFileInspector Members

        public string FileExtension
        {
            get { return ".map"; }
        }

        public string FileTypeDescription
        {
            get { return "Cakewalk Drum Map"; }
        }

        public string Describe(string fileName)
        {
            CakewalkMapFile mapFile = new CakewalkMapFile(fileName);
            return mapFile.ToString();	
        }

        #endregion
    }
}
