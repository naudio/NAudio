
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// Provides the base services of a Core Foundation object: <br />
/// - Lifetime <br />
/// - Value Equality <br />
/// - Hash code <br />
/// - Description <br />
/// - Allocator retrieval
/// </summary>
internal abstract class AbstractCoreFoundationObject
        : CriticalHandle, IEquatable<AbstractCoreFoundationObject>
{
    /// <summary>
    /// Creates a new Core Foundation wrapper instance by the specified pointer to the actual Core Foundation object.
    /// </summary>
    /// <param name="handle">The pointer to the wrapping Core Foundation object.</param>
    protected AbstractCoreFoundationObject(IntPtr handle) : this(handle, false) { }

    /// <summary>
    /// Creates a new Core Foundation wrapper instance by the specified pointer to the actual Core Foundation object,
    /// as well as a value whether to add a reference to the input object before this constructor
    /// completes execution.
    /// </summary>
    /// <remarks>
    /// Passing <paramref name="addReference"/> with value <see langword="true"/> is useful 
    /// when a Core Foundation method uses <see href="https://developer.apple.com/library/archive/documentation/CoreFoundation/Conceptual/CFMemoryMgmt/Concepts/Ownership.html#//apple_ref/doc/uid/20001148-SW1">The Get Rule</see>.
    /// </remarks>
    /// <param name="handle">The pointer to the wrapping Core Foundation object.</param>
    /// <param name="addReference">A value whether to add an additional reference to <paramref name="handle"/> or not.</param>
    protected AbstractCoreFoundationObject(IntPtr handle, bool addReference) : base(IntPtr.Zero)
    {
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Handle had the value 0.");
        }
        else
        {
            this.handle = handle;
            if (addReference)
            {
                _ = NativeMethods.CFRetain(handle);
            }
        }
    }

    /// <summary>
    /// Gets the reference count of this Core Foundation object.
    /// </summary>
    public long ReferenceCount
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return NativeMethods.CFGetRetainCount(handle).Value.ToInt64();
        }
    }

    /// <summary>
    /// Internal helper for throwing exceptions if the 
    /// current object has been destroyed.
    /// </summary>
    [StackTraceHidden]
    protected void ThrowIfInvalidOrDisposed()
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        if (IsInvalid)
        {
            throw new InvalidOperationException("This Core Foundation object is invalidated.");
        }
    }

    /// <summary>
    /// Provides the wrapping Core Foundation object pointer. <br />
    /// Use it only when interoperating with native macOS API's.
    /// </summary>
    /// <returns>The raw Core Foundation pointer.</returns>
    public IntPtr DangerousGetObject() => handle;

    /// <summary>
    /// Provides the wrapping Core Foundation object pointer. <br />
    /// Use it only when interoperating with native macOS API's.
    /// </summary>
    /// <returns>The raw Core Foundation pointer.</returns>
    /// <remarks>This property does also validate whether the current object is still valid before handing the value.</remarks>
    public IntPtr NativeObject
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return handle;
        }
    }

    /*
    mdcdi1315: Keeping the below two methods commented for now -
    if CoreMIDI support needs it, we will uncomment them as required.

    /// <summary>
    /// Adds a reference to the reference count of this Core Foundation object.
    /// </summary>
    public void AddReference()
    {
        Monitor.Enter(this);
        try
        {
            ThrowIfInvalidOrDisposed();
            _ = NativeMethods.CFRetain(handle);
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    /// <summary>
    /// Removes a reference to the reference count of this Core Foundation object.
    /// </summary>
    public void RemoveReference()
    {
        Monitor.Enter(this);
        try
        {
            ThrowIfInvalidOrDisposed();
            nint handleValue = this.handle;
            if (ReferenceCount == 1L)
            {
                // This means that the object is going to be released. 
                // As such, set the object as invalid.
                SetHandleAsInvalid();
            }
            NativeMethods.CFRelease(handleValue);
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    */

    /// <summary>
    /// Determines whether two Core Foundation objects are considered equal.
    /// </summary>
    /// <param name="other">The other Core Foundation object to compare against.</param>
    /// <returns><see langword="true"/> if <see langword="this"/> and <paramref name="other"/> are of the same type and considered equal, otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Equality is something specific to each Core Foundation opaque type. 
    /// For example, two CFNumber objects are equal if the numeric values they represent are equal. 
    /// Two <see cref="CFString"/> objects are equal if they represent identical sequences of characters, regardless of encoding.
    /// </remarks>
    public bool Equals(AbstractCoreFoundationObject other)
    {
        ThrowIfInvalidOrDisposed();
        return other is not null &&
               (!(other.IsInvalid || other.IsClosed)) &&
               NativeMethods.CFEqual(handle, other.handle) != MacBoolean.False;
    }

    public sealed override bool Equals(object obj) => (!(IsInvalid || IsClosed)) && Equals(obj as AbstractCoreFoundationObject);

    public sealed override bool IsInvalid => handle == IntPtr.Zero;

    public sealed override int GetHashCode() => IsClosed || IsInvalid ?
        base.GetHashCode() :
        NativeMethods.CFHash(handle).Value.ToUInt64().GetHashCode();

    /// <summary>
    /// Returns a textual description of this Core Foundation object by using the
    /// <c>CFCopyDescription</c> function.
    /// </summary>
    /// <remarks>
    /// The nature of the description differs by object. 
    /// For example, a description of a CFArray object would include descriptions of each of the elements in the collection. <br /> <br />
    /// 
    /// You can use this method for debugging Core Foundation objects in your code. 
    /// Note, however, that the description for a given object may be different in different releases of the operating system. 
    /// Do not create dependencies in your code on the content or format of the information returned by this function.
    /// </remarks>
    /// <returns>A description of this object, if not still disposed of.</returns>
    public override string ToString()
    {
        if (IsClosed || IsInvalid)
        {
            return base.ToString();
        }
        else
        {
            using CFString s = new(NativeMethods.CFCopyDescription(handle));
            return s.ToString();
        }
    }

    /// <summary>
    /// Returns the allocator used to allocate the current Core Foundation object.
    /// </summary>
    /// <remarks>The returned allocator must be disposed by using the <see cref="CriticalHandle.Dispose()"/> method.</remarks>
    /// <returns>The allocator used to allocate memory for the current Core Foundation object.</returns>
    public CFAllocator GetAllocator()
    {
        ThrowIfInvalidOrDisposed();
        return new CFAllocator(
            NativeMethods.CFGetAllocator(handle),
            true
        );
    }

    protected sealed override bool ReleaseHandle()
    {
        NativeMethods.CFRelease(handle);
        return true;
    }
}