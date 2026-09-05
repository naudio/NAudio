namespace NAudio.MacOS;

/// <summary>
/// MacOS definition of the logical boolean type. <br />
/// This type ensures that the boolean marshalling is correctly performed when so needed. <br />
/// It is defined as a byte (<c>unsigned char</c> in code) and contains two constants, <see cref="False"/> and <see cref="True"/>.
/// </summary>
/// <remarks>It is declared as: <c>typedef unsigned char Boolean;</c></remarks>
internal enum MacBoolean : System.Byte
{
    False = 0,
    True = 1
}