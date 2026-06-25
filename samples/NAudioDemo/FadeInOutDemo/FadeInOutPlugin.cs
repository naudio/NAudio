namespace NAudioDemo.FadeInOutDemo;

internal class FadeInOutPlugin : INAudioDemoPlugin
{
    public string Name
    {
        get { return "Fade In Out"; }
    }

    public System.Windows.Forms.Control CreatePanel()
    {
        return new FadeInOutPanel();
    }
}
