using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// See mfobjects.h
    /// </summary>
    public enum MediaEventType
    {
        /// <summary>
        /// Unknown event type.
        /// </summary>
        MEUnknown = 0,
        /// <summary>
        /// Signals a serious error.
        /// </summary>
        MEError = 1,
        /// <summary>
        /// Custom event type.
        /// </summary>
        MEExtendedType = 2,
        /// <summary>
        /// A non-fatal error occurred during streaming.
        /// </summary>
        MENonFatalError = 3,
        // MEGenericV1Anchor = MENonFatalError,
        /// <summary>
        /// Session Unknown
        /// </summary>
        MESessionUnknown = 100,
        /// <summary>
        /// Raised after the IMFMediaSession::SetTopology method completes asynchronously
        /// </summary>
        MESessionTopologySet = 101,
        /// <summary>
        /// Raised by the Media Session when the IMFMediaSession::ClearTopologies method completes asynchronously.
        /// </summary>
        MESessionTopologiesCleared = 102,
        /// <summary>
        /// Raised when the IMFMediaSession::Start method completes asynchronously.
        /// </summary>
        MESessionStarted = 103,
        /// <summary>
        /// Raised when the IMFMediaSession::Pause method completes asynchronously.
        /// </summary>
        MESessionPaused = 104,
        /// <summary>
        /// Raised when the IMFMediaSession::Stop method completes asynchronously.
        /// </summary>
        MESessionStopped = 105,
        /// <summary>
        /// Raised when the IMFMediaSession::Close method completes asynchronously.
        /// </summary>
        MESessionClosed = 106,
        /// <summary>
        /// Raised by the Media Session when it has finished playing the last presentation in the playback queue.
        /// </summary>
        MESessionEnded = 107,
        /// <summary>
        /// Raised by the Media Session when the playback rate changes.
        /// </summary>
        MESessionRateChanged = 108,
        /// <summary>
        /// Raised by the Media Session when it completes a scrubbing request.
        /// </summary>
        MESessionScrubSampleComplete = 109,
        /// <summary>
        /// Raised by the Media Session when the session capabilities change.
        /// </summary>
        MESessionCapabilitiesChanged = 110,
        /// <summary>
        /// Raised by the Media Session when the status of a topology changes.
        /// </summary>
        MESessionTopologyStatus = 111,
        /// <summary>
        /// Raised by the Media Session when a new presentation starts.
        /// </summary>
        MESessionNotifyPresentationTime = 112,
        /// <summary>
        /// Raised by a media source a new presentation is ready.
        /// </summary>
        MENewPresentation = 113,
        /// <summary>
        /// License acquisition is about to begin.
        /// </summary>
        MELicenseAcquisitionStart = 114,
        /// <summary>
        /// License acquisition is complete.
        /// </summary>
        MELicenseAcquisitionCompleted = 115,
        /// <summary>
        /// Individualization is about to begin.
        /// </summary>
        MEIndividualizationStart = 116,
        /// <summary>
        /// Individualization is complete.
        /// </summary>
        MEIndividualizationCompleted = 117,
        /// <summary>
        /// Signals the progress of a content enabler object.
        /// </summary>
        MEEnablerProgress = 118,
        /// <summary>
        /// A content enabler object's action is complete.
        /// </summary>
        MEEnablerCompleted = 119,
        /// <summary>
        /// Raised by a trusted output if an error occurs while enforcing the output policy.
        /// </summary>
        MEPolicyError = 120,
        /// <summary>
        /// Contains status information about the enforcement of an output policy.
        /// </summary>
        MEPolicyReport = 121,
        /// <summary>
        /// A media source started to buffer data.
        /// </summary>
        MEBufferingStarted = 122,

        /// <summary>
        /// A media source stopped buffering data.
        /// </summary>
        MEBufferingStopped = 123,

        /// <summary>
        /// The network source started opening a URL.
        /// </summary>
        MEConnectStart = 124,
        /// <summary>
        /// The network source finished opening a URL.
        /// </summary>
        MEConnectEnd = 125,
        /// <summary>
        /// Raised by a media source at the start of a reconnection attempt.
        /// </summary>
        MEReconnectStart = 126,
        /// <summary>
        /// Raised by a media source at the end of a reconnection attempt.
        /// </summary>
        MEReconnectEnd = 127,
        /// <summary>
        /// Raised by the enhanced video renderer (EVR) when it receives a user event from the presenter.
        /// </summary>
        MERendererEvent = 128,
        /// <summary>
        /// Raised by the Media Session when the format changes on a media sink.
        /// </summary>
        MESessionStreamSinkFormatChanged = 129,
        //MESessionV1Anchor = MESessionStreamSinkFormatChanged,
        /// <summary>
        /// Source Unknown
        /// </summary>
        MESourceUnknown = 200,
        /// <summary>
        /// Raised when a media source starts without seeking.
        /// </summary>
        MESourceStarted = 201,
        /// <summary>
        /// Raised by a media stream when the source starts without seeking.
        /// </summary>
        MEStreamStarted = 202,
        /// <summary>
        /// Raised when a media source seeks to a new position.
        /// </summary>
        MESourceSeeked = 203,
        /// <summary>
        /// Raised by a media stream after a call to IMFMediaSource::Start causes a seek in the stream.
        /// </summary>
        MEStreamSeeked = 204,
        /// <summary>
        /// Raised by a media source when it starts a new stream.
        /// </summary>
        MENewStream = 205,
        /// <summary>
        /// Raised by a media source when it restarts or seeks a stream that is already active.
        /// </summary>
        MEUpdatedStream = 206,
        /// <summary>
        /// Raised by a media source when the IMFMediaSource::Stop method completes asynchronously.
        /// </summary>
        MESourceStopped = 207,
        /// <summary>
        /// Raised by a media stream when the IMFMediaSource::Stop method completes asynchronously.
        /// </summary>
        MEStreamStopped = 208,
        /// <summary>
        /// Raised by a media source when the IMFMediaSource::Pause method completes asynchronously.
        /// </summary>
        MESourcePaused = 209,
        /// <summary>
        /// Raised by a media stream when the IMFMediaSource::Pause method completes asynchronously.
        /// </summary>
        MEStreamPaused = 210,
        /// <summary>
        /// Raised by a media source when a presentation ends.
        /// </summary>
        MEEndOfPresentation = 211,
        /// <summary>
        /// Raised by a media stream when the stream ends.
        /// </summary>
        MEEndOfStream = 212,
        /// <summary>
        /// Raised when a media stream delivers a new sample.
        /// </summary>
        MEMediaSample = 213,
        /// <summary>
        /// Signals that a media stream does not have data available at a specified time.
        /// </summary>
        MEStreamTick = 214,
        /// <summary>
        /// Raised by a media stream when it starts or stops thinning the stream.
        /// </summary>
        MEStreamThinMode = 215,
        /// <summary>
        /// Raised by a media stream when the media type of the stream changes.
        /// </summary>
        MEStreamFormatChanged = 216,
        /// <summary>
        /// Raised by a media source when the playback rate changes.
        /// </summary>
        MESourceRateChanged = 217,
        /// <summary>
        /// Raised by the sequencer source when a segment is completed and is followed by another segment.
        /// </summary>
        MEEndOfPresentationSegment = 218,
        /// <summary>
        /// Raised by a media source when the source's characteristics change.
        /// </summary>
        MESourceCharacteristicsChanged = 219,
        /// <summary>
        /// Raised by a media source to request a new playback rate.
        /// </summary>
        MESourceRateChangeRequested = 220,
        /// <summary>
        /// Raised by a media source when it updates its metadata.
        /// </summary>
        MESourceMetadataChanged = 221,
        /// <summary>
        /// Raised by the sequencer source when the IMFSequencerSource::UpdateTopology method completes asynchronously.
        /// </summary>
        MESequencerSourceTopologyUpdated = 222,
        //MESourceV1Anchor = MESequencerSourceTopologyUpdated,
        /// <summary>
        /// Sink Unknown
        /// </summary>
        MESinkUnknown = 300,
        /// <summary>
        /// Raised by a stream sink when it completes the transition to the running state.
        /// </summary>
        MEStreamSinkStarted = 301,
        /// <summary>
        /// Raised by a stream sink when it completes the transition to the stopped state.
        /// </summary>
        MEStreamSinkStopped = 302,
        /// <summary>
        /// Raised by a stream sink when it completes the transition to the paused state.
        /// </summary>
        MEStreamSinkPaused = 303,
        /// <summary>
        /// Raised by a stream sink when the rate has changed.
        /// </summary>
        MEStreamSinkRateChanged = 304,
        /// <summary>
        /// Raised by a stream sink to request a new media sample from the pipeline.
        /// </summary>
        MEStreamSinkRequestSample = 305,
        /// <summary>
        /// Raised by a stream sink after the IMFStreamSink::PlaceMarker method is called.
        /// </summary>
        MEStreamSinkMarker = 306,
        /// <summary>
        /// Raised by a stream sink when the stream has received enough preroll data to begin rendering.
        /// </summary>
        MEStreamSinkPrerolled = 307,
        /// <summary>
        /// Raised by a stream sink when it completes a scrubbing request.
        /// </summary>
        MEStreamSinkScrubSampleComplete = 308,
        /// <summary>
        /// Raised by a stream sink when the sink's media type is no longer valid.
        /// </summary>
        MEStreamSinkFormatChanged = 309,
        /// <summary>
        /// Raised by the stream sinks of the EVR if the video device changes.
        /// </summary>
        MEStreamSinkDeviceChanged = 310,
        /// <summary>
        /// Provides feedback about playback quality to the quality manager.
        /// </summary>
        MEQualityNotify = 311,
        /// <summary>
        /// Raised when a media sink becomes invalid.
        /// </summary>
        MESinkInvalidated = 312,

        /// <summary>
        /// The audio session display name changed.
        /// </summary>
        MEAudioSessionNameChanged = 313,

        /// <summary>
        /// The volume or mute state of the audio session changed
        /// </summary>
        MEAudioSessionVolumeChanged = 314,

        /// <summary>
        /// The audio device was removed.
        /// </summary>
        MEAudioSessionDeviceRemoved = 315,

        /// <summary>
        /// The Windows audio server system was shut down.
        /// </summary>
        MEAudioSessionServerShutdown = 316,

        /// <summary>
        /// The grouping parameters changed for the audio session.
        /// </summary>
        MEAudioSessionGroupingParamChanged = 317,

        /// <summary>
        /// The audio session icon changed.
        /// </summary>
        MEAudioSessionIconChanged = 318,

        /// <summary>
        /// The default audio format for the audio device changed.
        /// </summary>
        MEAudioSessionFormatChanged = 319,

        /// <summary>
        /// The audio session was disconnected from a Windows Terminal Services session
        /// </summary>
        MEAudioSessionDisconnected = 320,

        /// <summary>
        /// The audio session was preempted by an exclusive-mode connection.
        /// </summary>
        MEAudioSessionExclusiveModeOverride = 321,
        //MESinkV1Anchor = MEAudioSessionExclusiveModeOverride,
        /// <summary>
        /// Trust Unknown
        /// </summary>
        METrustUnknown = 400,
        /// <summary>
        /// The output policy for a stream changed.
        /// </summary>
        MEPolicyChanged = 401,
        /// <summary>
        /// Content protection message
        /// </summary>
        MEContentProtectionMessage = 402,
        /// <summary>
        /// The IMFOutputTrustAuthority::SetPolicy method completed.
        /// </summary>
        MEPolicySet = 403,
        //METrustV1Anchor = MEPolicySet,
        /// <summary>
        /// DRM License Backup Completed
        /// </summary>
        MEWMDRMLicenseBackupCompleted = 500,
        /// <summary>
        /// DRM License Backup Progress
        /// </summary>
        MEWMDRMLicenseBackupProgress = 501,
        /// <summary>
        /// DRM License Restore Completed
        /// </summary>
        MEWMDRMLicenseRestoreCompleted = 502,
        /// <summary>
        /// DRM License Restore Progress
        /// </summary>
        MEWMDRMLicenseRestoreProgress = 503,
        /// <summary>
        /// DRM License Acquisition Completed
        /// </summary>
        MEWMDRMLicenseAcquisitionCompleted = 506,
        /// <summary>
        /// DRM Individualization Completed
        /// </summary>
        MEWMDRMIndividualizationCompleted = 508,
        /// <summary>
        /// DRM Individualization Progress
        /// </summary>
        MEWMDRMIndividualizationProgress = 513,
        /// <summary>
        /// DRM Proximity Completed
        /// </summary>
        MEWMDRMProximityCompleted = 514,
        /// <summary>
        /// DRM License Store Cleaned
        /// </summary>
        MEWMDRMLicenseStoreCleaned = 515,
        /// <summary>
        /// DRM Revocation Download Completed
        /// </summary>
        MEWMDRMRevocationDownloadCompleted = 516,
        //MEWMDRMV1Anchor = MEWMDRMRevocationDownloadCompleted,
        /// <summary>
        /// Transform Unknown
        /// </summary>
        METransformUnknown = 600,
        /// <summary>
        /// Sent by an asynchronous MFT to request a new input sample.
        /// </summary>
        METransformNeedInput = (METransformUnknown + 1),
        /// <summary>
        /// Sent by an asynchronous MFT when new output data is available from the MFT.
        /// </summary>
        METransformHaveOutput = (METransformNeedInput + 1),
        /// <summary>
        /// Sent by an asynchronous Media Foundation transform (MFT) when a drain operation is complete.
        /// </summary>
        METransformDrainComplete = (METransformHaveOutput + 1),
        /// <summary>
        /// Sent by an asynchronous MFT in response to an MFT_MESSAGE_COMMAND_MARKER message.
        /// </summary>
        METransformMarker = (METransformDrainComplete + 1),
        //MEReservedMax = 10000
    }
}


/*

MECaptureAudioSessionDeviceRemoved 	The device was removed.
MECaptureAudioSessionDisconnected 	The audio session is disconnected because the user logged off from a Windows Terminal Services (WTS) session.
MECaptureAudioSessionExclusiveModeOverride 	The user opened an audio stream in exclusive mode.
MECaptureAudioSessionFormatChanged 	The audio format changed.
MECaptureAudioSessionServerShutdown 	The audio session server shutdown.
MECaptureAudioSessionVolumeChanged 	The volume changed.
MEContentProtectionMessage 	The configuration changed for an output protection scheme.
MEVideoCaptureDevicePreempted 	The device has been preempted.
MEVideoCaptureDeviceRemoved 	The device has been removed.
}*/
