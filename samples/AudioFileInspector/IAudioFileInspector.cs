using System;
using System.Collections.Generic;
using System.Text;

namespace AudioFileInspector
{
    public interface IAudioFileInspector
    {
        string FileExtension { get; }
        string FileTypeDescription { get; }
        string Describe(string fileName);
    }
}
