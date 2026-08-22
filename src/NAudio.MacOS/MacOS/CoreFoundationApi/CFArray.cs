// This interop definition was derived from the file CFArray.h of the Core Foundation Framework.
// See https://developer.apple.com/documentation/corefoundation for more information.

/*!
	@header CFArray
	CFArray implements an ordered, compact container of pointer-sized
	values. Values are accessed via integer keys (indices), from the
	range 0 to N-1, where N is the number of values in the array when
	an operation is performed. The array is said to be "compact" because
	deleted or inserted values do not leave a gap in the key space --
	the values with higher-numbered indices have their indices
	renumbered lower (or higher, in the case of insertion) so that the
	set of valid indices is always in the integer range [0, N-1]. Thus,
	the index to access a particular value in the array may change over
	time as other values are inserted into or deleted from the array.

	Arrays come in two flavors, immutable, which cannot have values
	added to them or removed from them after the array is created, and
	mutable, to which you can add values or from which remove values.
	Mutable arrays can have an unlimited number of values (or rather,
	limited only by constraints external to CFArray, like the amount
	of available memory).

	As with all CoreFoundation collection types, arrays maintain hard
	references on the values you put in them, but the retaining and
	releasing functions are user-defined callbacks that can actually do
	whatever the user wants (for example, nothing).

	Computational Complexity
	The access time for a value in the array is guaranteed to be at
	worst O(lg N) for any implementation, current and future, but will
	often be O(1) (constant time). Linear search operations similarly
	have a worst case complexity of O(N*lg N), though typically the
	bounds will be tighter, and so on. Insertion or deletion operations
	will typically be linear in the number of values in the array, but
	may be O(N*lg N) clearly in the worst case in some implementations.
	There are no favored positions within the array for performance;
	that is, it is not necessarily faster to access values with low
	indices, or to insert or delete values with high indices, or
	whatever.
*/

using System;
using System.Collections.Generic;

namespace NAudio.MacOS.CoreFoundationApi;

internal sealed class CFArray : AbstractCoreFoundationObject, ICloneable
{
    public CFArray(nint handle) : base(handle) { }

    public CFArray(nint handle, bool addReference) : base(handle, addReference) { }

    public long Count
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return NativeMethods.CFArrayGetCount(handle).Value.ToInt64();
        }
    }

    public IntPtr GetItem(int index)
    {
        ThrowIfInvalidOrDisposed();
        return NativeMethods.CFArrayGetValueAtIndex(handle, new(index));
    }

    public IntPtr GetItem(long index)
    {
        ThrowIfInvalidOrDisposed();
        return NativeMethods.CFArrayGetValueAtIndex(handle, new(new nint(index)));
    }

    public unsafe IntPtr[] GetItems()
    {
        ThrowIfInvalidOrDisposed();
        IntPtr[] retArray = new IntPtr[Count];
        fixed (IntPtr* pArray = retArray)
        {
            NativeMethods.CFArrayGetValues(handle, new(0L, retArray.LongLength), new(pArray));
        }
        return retArray;
    }

    public IEnumerable<IntPtr> GetItemsAsEnumerable()
    {
        long c = Count;
        for (long I = 0L; I < c; I++)
        {
            yield return NativeMethods.CFArrayGetValueAtIndex(handle, new(new IntPtr(I)));
        }
    }

    public CFArray Clone()
    {
        ThrowIfInvalidOrDisposed();
        using var allocator = GetAllocator();
        return new(NativeMethods.CFArrayCreateCopy(allocator.NativeObject, handle));
    }

    object ICloneable.Clone() => Clone();
}