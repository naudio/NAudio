using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Utils;

namespace NAudio.Dmo
{
    /// <summary>
    /// From wmcodecsdp.h
    /// Implements:
    /// - IMediaObject 
    /// - IMFTransform (Media foundation - we will leave this for now as there is loads of MF stuff)
    /// - IPropertyStore 
    /// - IWMResamplerProps 
    /// Can resample PCM or IEEE
    /// </summary>
    [ComImport, Guid("f447b69e-1884-4a7e-8055-346f74d6edb3")]
    class ResamplerMediaObject
    {
    }

    /// <summary>
    /// Resampler
    /// </summary>
    public class Resampler
    {
        MediaObject mediaObject;
        IPropertyStore propertyStoreInterface;
        IWMResamplerProps resamplerPropsInterface;

        /// <summary>
        /// Creates a new Resampler based on the DMO Resampler
        /// </summary>
        public Resampler()
        {
            ResamplerMediaObject mediaComObject = new ResamplerMediaObject();
            mediaObject = new MediaObject((IMediaObject)mediaComObject);
            propertyStoreInterface = (IPropertyStore)mediaComObject;
            resamplerPropsInterface = (IWMResamplerProps)mediaComObject;
        }

        /// <summary>
        /// Media Object
        /// </summary>
        public MediaObject MediaObject
        {
            get
            {
                return mediaObject;
            }
        }

    }
}
