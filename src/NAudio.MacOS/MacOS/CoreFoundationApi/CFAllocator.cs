
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// CFAllocator is an opaque type that allocates and deallocates memory for you. 
/// You never have to allocate, reallocate, or deallocate memory directly for Core Foundation objects—and rarely should you. 
/// You pass CFAllocator objects into functions that create objects; these functions have “Create” embedded in their names, for example, CFStringCreateWithPascalString. 
/// The creation functions use the allocators to allocate memory for the objects they create.
/// </summary>
internal sealed class CFAllocator : AbstractCoreFoundationObject
{
    public CFAllocator(nint handle) : base(handle) { }

    public CFAllocator(nint handle, bool addReference) : base(handle, addReference) { }

    /// <summary>
    /// Gets the default allocator object for the current thread.
    /// </summary>
    /// <remarks>The returned object must be disposed of by using the <see cref="CriticalHandle.Dispose()"/> method.</remarks>
    /// <returns>A reference to the default allocator for the current thread. If none has been explicitly set, returns the generic system allocator, kCFAllocatorSystemDefault.</returns>
    [return: NotNull]
    public static CFAllocator GetDefault() => new(NativeMethods.CFAllocatorGetDefault(), true);

    /// <summary>Allocates memory.</summary>
    /// <param name="size">The size of the memory to allocate.</param>
    /// <returns>A pointer to the newly allocated memory.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
    public IntPtr Allocate(IntPtr size)
    {
        ThrowIfInvalidOrDisposed();
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");
        }
        else
        {
            CLong actualSize = NativeMethods.CFAllocatorGetPreferredSizeForSize(handle, new(size));
            return NativeMethods.CFAllocatorAllocate(handle, actualSize);
        }
    }

    /// <summary>Deallocates a block of memory.</summary>
    /// <param name="pointer">An untyped pointer to a block of memory to deallocate using allocator.</param>
    /// <remarks>You must use the same allocator to deallocate memory as was used to allocate it.</remarks>
    public void Deallocate(IntPtr pointer)
    {
        ThrowIfInvalidOrDisposed();
        if (pointer != IntPtr.Zero)
        {
            NativeMethods.CFAllocatorDeallocate(handle, pointer);
        }
    }

    /// <summary>Reallocates memory.</summary>
    /// <param name="oldPointer">
    /// An untyped pointer to a block of memory to reallocate to a new size. 
    /// If <paramref name="oldPointer"/> is <see langword="null" /> and <paramref name="newSize"/> is greater than 0, memory is allocated (using the allocate function callback of the allocator’s context).
    /// If <paramref name="oldPointer"/> is <see langword="null" /> and <paramref name="newSize"/> is 0, the result is <see langword="null" />.</param>
    /// <param name="newSize">
    /// The number of bytes to allocate. 
    /// If you pass 0 and the <paramref name="oldPointer"/> parameter is non-<see langword="null" />, the block of memory that <paramref name="oldPointer"/> points to is typically deallocated. 
    /// If you pass 0 for this parameter and the <paramref name="oldPointer"/> parameter is <see langword="null" />, nothing happens and the result returned is <see langword="null" />.
    /// </param>
    /// <returns>
    /// An untyped pointer to a block of memory or <see langword="null" />. 
    /// A <see langword="null" /> result indicates either a failure to allocate memory or some other outcome, the precise interpretation of which is determined by the values of certain parameters and the presence or absence of callbacks in the allocator context. 
    /// </returns>
    /// <remarks>
    /// The <see cref="Reallocate" /> method’s primary purpose is to reallocate a block of memory to a new (and usually larger) size. 
    /// However, based on the values passed in certain of the parameters, this function can also allocate memory afresh or deallocate a given block of memory. 
    /// The following summarizes the semantic combinations: <br /> <br />
    /// 
    /// If the <paramref name="oldPointer"/> parameter is non-<see langword="null" /> and the <paramref name="newSize"/> parameter is greater than 0, the behavior is to reallocate. <br /> <br />
    /// 
    /// If the <paramref name="oldPointer"/> parameter is <see langword="null" /> and the <paramref name="newSize"/> parameter is greater than 0, the behavior is to allocate. <br /> <br />
    /// 
    /// If the <paramref name="oldPointer"/> parameter is non-<see langword="null" /> and the <paramref name="newSize"/> parameter is 0, the behavior is to deallocate.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="newSize"/> is less than 0.</exception>
    [return: MaybeNull]
    public IntPtr Reallocate([AllowNull] IntPtr oldPointer, nint newSize)
    {
        ThrowIfInvalidOrDisposed();
        if (newSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), "Size cannot be negative.");
        }
        else
        {
            CLong actualSize = NativeMethods.CFAllocatorGetPreferredSizeForSize(handle, new(newSize));
            return NativeMethods.CFAllocatorReallocate(handle, oldPointer, actualSize);
        }
    }
}