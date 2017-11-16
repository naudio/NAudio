# Enumerate Media Foundation Transforms

The `MediaFoundationReader` and `MediaFoundationEncoder` classes in NAudio make use of any available Media Foundation Transforms installed on your computer. It can be useful to enumerate any audio related MFTs on your computer.

There are three types of audio MFT - effects, decoders and encoders. A decoder allows you to decode audio compressed in different formats to PCM. An encoder allows you to encode PCM audio into compressed formats. An effect modifies audio in some way. The most 

You can use `MediaFoundationApi.EnumerateTransforms` to explore 

```c#
var effects = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect);

var decoders = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioDecoder);

var encoder = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEncoder);
```

These return an `IEnumerable<IMFActivate>`. This is a fairly low-level interface. Here's some code that will describe an `IMFActivate` by exploring its attributes:

```c#
private string DescribeMft(IMFActivate mft)
{
    mft.GetCount(out var attributeCount);
    var sb = new StringBuilder();
    for (int n = 0; n < attributeCount; n++)
    {
        AddAttribute(mft, n, sb);
    }
    return sb.ToString();
}

private static void AddAttribute(IMFActivate mft, int index, StringBuilder sb)
{
    var variantPtr = Marshal.AllocHGlobal(MarshalHelpers.SizeOf<PropVariant>());
    try
    {
        mft.GetItemByIndex(index, out var key, variantPtr);
        var value = MarshalHelpers.PtrToStructure<PropVariant>(variantPtr);
        var propertyName = FieldDescriptionHelper.Describe(typeof (MediaFoundationAttributes), key);
        if (key == MediaFoundationAttributes.MFT_INPUT_TYPES_Attributes ||
            key == MediaFoundationAttributes.MFT_OUTPUT_TYPES_Attributes)
        {
            var types = value.GetBlobAsArrayOf<MFT_REGISTER_TYPE_INFO>();
            sb.AppendFormat("{0}: {1} items:", propertyName, types.Length);
            sb.AppendLine();
            foreach (var t in types)
            {
                sb.AppendFormat("    {0}-{1}",
                    FieldDescriptionHelper.Describe(typeof (MediaTypes), t.guidMajorType),
                    FieldDescriptionHelper.Describe(typeof (AudioSubtypes), t.guidSubtype));
                sb.AppendLine();
            }
        }
        else if (key == MediaFoundationAttributes.MF_TRANSFORM_CATEGORY_Attribute)
        {
            sb.AppendFormat("{0}: {1}", propertyName,
                FieldDescriptionHelper.Describe(typeof (MediaFoundationTransformCategories), (Guid) value.Value));
            sb.AppendLine();
        }
        else if (value.DataType == (VarEnum.VT_VECTOR | VarEnum.VT_UI1))
        {
            var b = (byte[]) value.Value;
            sb.AppendFormat("{0}: Blob of {1} bytes", propertyName, b.Length);
            sb.AppendLine();
        }
        else
        {
            sb.AppendFormat("{0}: {1}", propertyName, value.Value);
            sb.AppendLine();
        }
    }
    finally
    {
        PropVariant.Clear(variantPtr);
        Marshal.FreeHGlobal(variantPtr);
    }
}
```

Here's an example output for an MFT effect. In this case, the Resampler which is a very useful MFT for changing sample rates:

```
Audio Effect
Name: Resampler MFT
Input Types: 2 items:
    Audio-PCM
    Audio-IEEE floating-point
Class identifier: f447b69e-1884-4a7e-8055-346f74d6edb3
Output Types: 2 items:
    Audio-PCM
    Audio-IEEE floating-point
Transform Flags: 1
Transform Category: Audio Effect
```

Here's an example output for a decoder. This shows Windows 10 can decode the Opus audio codec:

```
Audio Decoder
Name: Microsoft Opus Audio Decoder MFT
Input Types: 1 items:
    Audio-0000704f-0000-0010-8000-00aa00389b71
Class identifier: 63e17c10-2d43-4c42-8fe3-8d8b63e46a6a
Output Types: 1 items:
    Audio-IEEE floating-point
Transform Flags: 1
Transform Category: Audio Decoder
```

And an encoder. This is another one new to Windows 10 - it comes with a FLAC encoder:
```
Audio Encoder
Name: Microsoft FLAC Audio Encoder MFT
Input Types: 1 items:
    Audio-PCM
Class identifier: 128509e9-c44e-45dc-95e9-c255b8f466a6
Output Types: 1 items:
    Audio-0000f1ac-0000-0010-8000-00aa00389b71
Transform Flags: 1
Transform Category: Audio Encoder
```

