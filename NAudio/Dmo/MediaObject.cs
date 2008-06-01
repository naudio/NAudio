using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    public class MediaObject
    {
        IMediaObject mediaObject;
        int inputStreams;
        int outputStreams;

        internal MediaObject(IMediaObject mediaObject)
        {
            this.mediaObject = mediaObject;
            mediaObject.GetStreamCount(out inputStreams, out outputStreams);
        }

        public int InputStreamCount
        {
            get { return inputStreams; }
        }

        public int OutputStreamCount
        {
            get { return outputStreams; }
        }

        public DmoMediaType? GetInputType(int inputStream, int inputTypeIndex)
        {
            try
            {
                DmoMediaType mediaType;
                int hresult = mediaObject.GetInputType(inputStream, inputTypeIndex, out mediaType);
                if (hresult == HResult.S_OK)
                {
                    // this frees the format (if present)
                    // we should therefore come up with a way of marshaling the format
                    // into a completely managed structure
                    DmoInterop.MoFreeMediaType(ref mediaType);
                    return mediaType;
                }
            }
            catch (COMException e)
            {
                if (e.ErrorCode != (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
                {
                    throw;
                }
            }
            return null;
        }

        public DmoMediaType? GetOutputType(int outputStream, int outputTypeIndex)
        {
            try
            {
                DmoMediaType mediaType;
                int hresult = mediaObject.GetOutputType(outputStream, outputTypeIndex, out mediaType);
                if (hresult == HResult.S_OK)
                {
                    // this frees the format (if present)
                    // we should therefore come up with a way of marshaling the format
                    // into a completely managed structure
                    DmoInterop.MoFreeMediaType(ref mediaType);
                    return mediaType;
                }
            }
            catch (COMException e)
            {
                if (e.ErrorCode != (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
                {
                    throw;
                }
            }
            return null;
        }


        public IEnumerable<DmoMediaType> GetInputTypes(int inputStreamIndex)
        {
            int typeIndex = 0;
            DmoMediaType? mediaType;
            while ((mediaType = GetInputType(inputStreamIndex,typeIndex)) != null)
            {
                yield return mediaType.Value;
                typeIndex++;
            }
        }

        public IEnumerable<DmoMediaType> GetOutputTypes(int outputStreamIndex)
        {
            int typeIndex = 0;
            DmoMediaType? mediaType;
            while ((mediaType = GetOutputType(outputStreamIndex, typeIndex)) != null)
            {
                yield return mediaType.Value;
                typeIndex++;
            }
        }

        public bool SupportsInputType(int inputStreamIndex, DmoMediaType mediaType)
        {
            return SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
        }

        private bool SetInputType(int inputStreamIndex, DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            try
            {
                mediaObject.SetInputType(inputStreamIndex, ref mediaType, flags);
            }
            catch (COMException e)
            {
                if (e.ErrorCode == (int)DmoHResults.DMO_E_TYPE_NOT_ACCEPTED)
                {
                    return false;
                }
                throw;
            }
            return true;
        }

        public void SetInputType(int inputStreamIndex, DmoMediaType mediaType)
        {
            if(!SetInputType(inputStreamIndex,mediaType,DmoSetTypeFlags.None))
            {
                throw new ArgumentException("Media Type not supported");
            }
        }


    }
}
