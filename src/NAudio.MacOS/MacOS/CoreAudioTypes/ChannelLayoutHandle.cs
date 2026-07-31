
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// Provides a base class implemented by internal clients for decoding 
/// macOS audio channel layouts.
/// </summary>
internal abstract class ChannelLayoutHandle : SafeHandle
{
    private uint size;

    private sealed class NativeMemoryHandle : ChannelLayoutHandle
    {
        public unsafe NativeMemoryHandle(uint size)
            : base(true)
        {
            void* pointer = NativeMemory.Alloc(size);
            SetHandle(new(pointer), size);
            Unsafe.InitBlockUnaligned(pointer, 0, size);
        }

        protected override unsafe bool ReleaseHandle()
        {
            NativeMemory.Free(handle.ToPointer());
            return true;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelLayoutHandle"/> class,
    /// defining whether this wrapper instance owns the handle.
    /// </summary>
    /// <param name="ownsHandle">A value whether the handle is being owned by this wrapper.</param>
    public ChannelLayoutHandle(bool ownsHandle) : base(IntPtr.Zero, ownsHandle) => size = 0U;

    public static ChannelLayoutHandle Allocate(uint size) => new NativeMemoryHandle(size);

    internal AudioChannelLayoutTag Tag
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsInvalid || IsClosed, this);
            return AudioChannelLayout.GetAudioChannelLayoutTag(handle);
        }
    }

    internal AudioChannelBitmap BitMap
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsInvalid || IsClosed, this);
            return AudioChannelLayout.GetAudioChannelBitmap(handle);
        }
    }

    internal AudioChannelDescription[] Descriptions
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsInvalid || IsClosed, this);
            uint descriptionCount = AudioChannelLayout.GetNumberOfChannelDescriptions(handle);
            AudioChannelDescription[] descriptions = new AudioChannelDescription[descriptionCount];
            for (uint I = 0; I < descriptionCount; I++)
            {
                descriptions[I] = AudioChannelLayout.GetChannelDescription(handle, I);
            }
            return descriptions;
        }
    }

    public uint Size => size;

    public sealed override bool IsInvalid => handle == IntPtr.Zero;

    public void SetHandle(nint handle, uint size)
    {
        SetHandle(handle);
        this.size = size;
    }
}