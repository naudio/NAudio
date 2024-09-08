using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct CardInfo
{
	public int Card;			/* card number */
	public int Pad;			/* reserved for future (was type) */
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
	public string ID;		/* ID of card (user selectable) */
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
	public string Driver;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string Name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string LongName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
	public string Reserved_;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
	public string MixerName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string Components;
}