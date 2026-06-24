using System;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Property Store class, only supports reading properties at the moment.
    /// </summary>
    public class PropertyStore
    {
        private readonly IPropertyStore storeInterface;

        /// <summary>
        /// Property Count
        /// </summary>
        public int Count
        {
            get
            {
                CoreAudioException.ThrowIfFailed(storeInterface.GetCount(out var result));
                return result;
            }
        }

        /// <summary>
        /// Gets property by index
        /// </summary>
        /// <param name="index">Property index</param>
        /// <returns>The property</returns>
        public PropertyStoreProperty this[int index]
        {
            get
            {
                PropertyKey key = Get(index);
                return ReadProperty(key, (in PropertyKey k, in PropVariant pv) => new PropertyStoreProperty(k, pv.Value));
            }
        }

        /// <summary>
        /// Contains property guid
        /// </summary>
        /// <param name="key">Looks for a specific key</param>
        /// <returns>True if found</returns>
        public bool Contains(PropertyKey key)
        {
            return TryReadProperty(key, (in PropertyKey _, in PropVariant pv) => pv.DataType != VarType.VT_EMPTY, out var present) && present;
        }

        /// <summary>
        /// Checks if the property exists
        /// </summary>
        /// <param name="key">Looks for a specific key</param>
        /// <param name="obj">The value of the property wrapped in a type cast</param>
        /// <typeparam name="T">The type to cast the property as</typeparam>
        /// <returns>True if found</returns>
        public bool TryGetValue<T>(PropertyKey key, out T obj)
        {
            if (TryReadProperty(key, (in PropertyKey _, in PropVariant pv) => pv.Value, out var value) && value != null)
            {
                obj = (T)value;
                return true;
            }
            obj = default;
            return false;
        }

        /// <summary>
        /// Indexer by guid
        /// </summary>
        /// <param name="key">Property Key</param>
        /// <returns>Property or null if not found</returns>
        public PropertyStoreProperty this[PropertyKey key]
        {
            get
            {
                return ReadProperty(key, (in PropertyKey k, in PropVariant pv) => new PropertyStoreProperty(k, pv.Value));
            }
        }

        /// <summary>
        /// Gets property key at specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Property key</returns>
        public PropertyKey Get(int index)
        {
            CoreAudioException.ThrowIfFailed(storeInterface.GetAt(index, out var key));
            return key;
        }

        /// <summary>
        /// Gets property value at specified index. The returned struct's pointer-typed fields
        /// (LPWSTR/BLOB/CLSID) are not safe to read — use the indexer overloads instead, which
        /// resolve the value before clearing the underlying COM-allocated memory.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Property value</returns>
        [Obsolete("Returned PropVariant may contain dangling pointers. Use this[int] which resolves the value safely.")]
        public PropVariant GetValue(int index)
        {
            PropertyKey key = Get(index);
            return ReadProperty(key, (in PropertyKey _, in PropVariant pv) => pv);
        }

        /// <summary>
        /// Sets property value at specified key.
        /// </summary>
        /// <param name="key">Key of property to set.</param>
        /// <param name="value">Value to write.</param>
        public void SetValue(PropertyKey key, PropVariant value)
        {
            unsafe
            {
                PropVariant local = value;
                CoreAudioException.ThrowIfFailed(storeInterface.SetValue(in key, (IntPtr)(&local)));
            }
        }

        /// <summary>
        /// Saves a property change.
        /// </summary>
        public void Commit()
        {
            CoreAudioException.ThrowIfFailed(storeInterface.Commit());
        }

        /// <summary>
        /// Creates a new property store
        /// </summary>
        /// <param name="store">IPropertyStore COM interface</param>
        internal PropertyStore(IPropertyStore store)
        {
            storeInterface = store;
        }

        private delegate T PropVariantProjection<T>(in PropertyKey key, in PropVariant value);

        private T ReadProperty<T>(PropertyKey key, PropVariantProjection<T> project)
        {
            unsafe
            {
                PropVariant buffer = default;
                IntPtr ptr = (IntPtr)(&buffer);
                CoreAudioException.ThrowIfFailed(storeInterface.GetValue(in key, ptr));
                try
                {
                    return project(in key, in buffer);
                }
                finally
                {
                    PropVariant.Clear(ptr);
                }
            }
        }

        private bool TryReadProperty<T>(PropertyKey key, PropVariantProjection<T> project, out T result)
        {
            unsafe
            {
                PropVariant buffer = default;
                IntPtr ptr = (IntPtr)(&buffer);
                int hr = storeInterface.GetValue(in key, ptr);
                try
                {
                    if (hr < 0)
                    {
                        result = default;
                        return false;
                    }
                    result = project(in key, in buffer);
                    return true;
                }
                finally
                {
                    PropVariant.Clear(ptr);
                }
            }
        }
    }
}
