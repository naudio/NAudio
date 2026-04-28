using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// A snapshot of audio endpoints returned by
    /// <see cref="MMDeviceEnumerator.EnumerateAudioEndPoints"/>. Indexable and enumerable;
    /// each access materialises a fresh <see cref="MMDevice"/> wrapper around the same
    /// underlying COM endpoint. Dispose when finished to release the underlying
    /// <c>IMMDeviceCollection</c>.
    /// </summary>
    public class MMDeviceCollection : IEnumerable<MMDevice>, IDisposable
    {
        private IMMDeviceCollection mmDeviceCollection;

        /// <summary>
        /// Number of endpoints in this collection.
        /// </summary>
        public int Count
        {
            get
            {
                CoreAudioException.ThrowIfFailed(mmDeviceCollection.GetCount(out var result));
                return result;
            }
        }

        /// <summary>
        /// Returns the endpoint at <paramref name="index"/>. The returned <see cref="MMDevice"/>
        /// is a fresh wrapper — call <see cref="MMDevice.Dispose"/> when finished.
        /// </summary>
        public MMDevice this[int index]
        {
            get
            {
                CoreAudioException.ThrowIfFailed(mmDeviceCollection.Item(index, out var ptr));
                try
                {
                    var device = (IMMDevice)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        ptr, CreateObjectFlags.UniqueInstance);
                    return new MMDevice(device);
                }
                finally
                {
                    Marshal.Release(ptr);
                }
            }
        }

        internal MMDeviceCollection(IMMDeviceCollection parent)
        {
            mmDeviceCollection = parent;
        }

        /// <inheritdoc/>
        public IEnumerator<MMDevice> GetEnumerator()
        {
            for (int index = 0; index < Count; index++)
            {
                yield return this[index];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// Releases the underlying <c>IMMDeviceCollection</c>. Iterators previously
        /// obtained from this collection should no longer be used after disposal.
        /// </summary>
        public void Dispose()
        {
            if (mmDeviceCollection != null)
            {
                if ((object)mmDeviceCollection is ComObject co)
                {
                    co.FinalRelease();
                }
                mmDeviceCollection = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
