namespace AudioFileInspector;

public interface IAudioFileInspector
{
    string FileExtension { get; }
    string FileTypeDescription { get; }
    string Describe(string fileName);
}
