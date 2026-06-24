namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Property Store Property
    /// </summary>
    public struct PropertyStoreProperty
    {
        internal PropertyStoreProperty(PropertyKey key, object value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Property Key
        /// </summary>
        public PropertyKey Key { get; }

        /// <summary>
        /// Property Value
        /// </summary>
        public object Value { get; }
    }
}
