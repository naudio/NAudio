using System;
using System.Buffers;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NAudio.Utils
{
    /// <summary>
    /// Adds extension methods for the <see cref="ArrayPool{T}"/> class.
    /// </summary>
    public static class ArrayPoolExtensions
    {
        /// <summary>
        /// Provides the buffer rent context instance, returned via the <see cref="RentByContext{T}(ArrayPool{T}, int, bool)"/> extension method.
        /// </summary>
        /// <typeparam name="T">The type of the array elements the backed <see cref="ArrayPool{T}"/> manages.</typeparam>
        public sealed class RentContext<T> : IDisposable
        {
            /// <summary>The requested buffer size.</summary>
            public readonly int Size;
            /// <summary>A value whether the array will be cleared after the buffer will be returned back to the pool.</summary>
            public readonly bool ClearArray;

            private T[] buffer;
            private ArrayPool<T> pool;

            internal RentContext(ArrayPool<T> pool, int size, bool clear_array)
            {
                buffer = (this.pool = pool).Rent(Size = size);
                ClearArray = clear_array;
            }

            /// <summary>The buffer that was returned by <see cref="ArrayPool{T}.Rent(int)"/>.</summary>
            /// <remarks>
            /// <see cref="Buffer"/> is guaranteed to not be <see langword="null"/> unless 
            /// the <see cref="Dispose"/> method has been called on this instance.
            /// </remarks>
            [MaybeNull]
            public T[] Buffer => buffer;

            /// <summary>The <see cref="ArrayPool{T}"/> that rented the <see cref="Buffer"/>.</summary>
            /// <remarks>
            /// <see cref="Pool"/> is guaranteed to not be <see langword="null"/> unless 
            /// the <see cref="Dispose"/> method has been called on this instance.
            /// </remarks>
            [MaybeNull]
            public ArrayPool<T> Pool => pool;

            /// <summary>
            /// Gets the actual length of <see cref="Buffer"/>.
            /// </summary>
            /// <remarks>
            /// <see cref="ActualLength"/> is guaranteed to always return the length of <see cref="Buffer"/> unless 
            /// the <see cref="Dispose"/> method has been called on this instance, which in such case 0 is returned.
            /// </remarks>
            public int ActualLength => buffer is null ? 0 : buffer.Length;

            /// <summary>Implicit operator to directly acquire the buffer reference.</summary>
            /// <param name="c">The context to retrieve the rented buffer.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // The JIT should aggressively inline it, when possible.
            public static implicit operator T[](RentContext<T> c) => c.Buffer;

            /// <summary>Gets a reference to the 0th element of <see cref="Buffer"/>, if not yet freed.</summary>
            /// <remarks>The method will return <see cref="Unsafe.NullRef{T}"/> if <see cref="Dispose"/> has been called on this structure instance.</remarks>
            /// <returns>The reference at the 0th element of <see cref="Buffer"/>.</returns>
            [return: MaybeNull]
            public ref T GetPinnableReference()
            {
                if (Buffer is null)
                {
                    return ref Unsafe.NullRef<T>();
                }
                else
                {
                    return ref Buffer[0];
                }
            }

            /// <summary>
            /// Returns the rented buffer back to the pool, and then unreferences the pool that rented the buffer.
            /// </summary>
            public void Dispose()
            {
                // Guard the call if in case of multiple threads.
                Monitor.Enter(this);
                try
                {
                    if (buffer is not null)
                    {
                        pool.Return(buffer, ClearArray);
                        buffer = null;
                        pool = null;
                    }
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Retrieves a buffer that is at least the requested length, from the current 
        /// <see cref="ArrayPool{T}"/> instance, and returns a
        /// <see cref="RentContext{T}"/> instance, representing the rented buffer. <br />
        /// This method enables <see cref="ArrayPool{T}"/> buffer renting and returning 
        /// to be done via the <see langword="using"/> construct.
        /// </summary>
        /// <typeparam name="T">The type of the elements that the specified <see cref="ArrayPool{T}"/> rents.</typeparam>
        /// <param name="pool">The <see cref="ArrayPool{T}"/> that can rent buffers.</param>
        /// <param name="minimum_length">The minimum length of the array needed.</param>
        /// <param name="clear_array">Optional. 
        /// If <see langword="true"/> and if the pool will store the buffer to enable subsequent reuse, <see cref="RentContext{T}.Dispose"/>
        /// will additionally clear the <see cref="RentContext{T}.Buffer"/> of its contents so that a subsequent consumer 
        /// via <see cref="ArrayPool{T}.Rent(int)"/> will not see the previous consumer's content. 
        /// If <see langword="false"/> or if the pool will release the buffer, the array's contents are left unchanged.</param>
        /// <remarks>
        /// This buffer is loaned to the caller and should be returned via <see cref="RentContext{T}.Dispose"/> 
        /// so that it may be reused in subsequent usage of <see cref="ArrayPool{T}.Rent"/>.
        /// It is not a fatal error to not return a rented buffer, but failure to do so may lead to
        /// decreased application performance, as the pool may need to create a new buffer to replace
        /// the one lost.
        /// </remarks>
        /// <returns>
        /// A new <see cref="RentContext{T}"/> instance representing the rented buffer. 
        /// The buffer has been already rented and can be retrieved through the <see cref="RentContext{T}.Buffer"/> property.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum_length"/> is a negative value.</exception>
        public static RentContext<T> RentByContext<T>(this ArrayPool<T> pool, int minimum_length, bool clear_array = false)
        {
            if (minimum_length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum_length), "Size of the rented buffer cannot be less than zero.");
            }
            else
            {
                return new RentContext<T>(pool, minimum_length, clear_array);
            }
        }
    }
}
