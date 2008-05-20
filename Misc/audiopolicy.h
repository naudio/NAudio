

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0499 */
/* Compiler settings for audiopolicy.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 500
#endif

/* verify that the <rpcsal.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCSAL_H_VERSION__
#define __REQUIRED_RPCSAL_H_VERSION__ 100
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __audiopolicy_h__
#define __audiopolicy_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IAudioSessionEvents_FWD_DEFINED__
#define __IAudioSessionEvents_FWD_DEFINED__
typedef interface IAudioSessionEvents IAudioSessionEvents;
#endif 	/* __IAudioSessionEvents_FWD_DEFINED__ */


#ifndef __IAudioSessionControl_FWD_DEFINED__
#define __IAudioSessionControl_FWD_DEFINED__
typedef interface IAudioSessionControl IAudioSessionControl;
#endif 	/* __IAudioSessionControl_FWD_DEFINED__ */


#ifndef __IAudioSessionManager_FWD_DEFINED__
#define __IAudioSessionManager_FWD_DEFINED__
typedef interface IAudioSessionManager IAudioSessionManager;
#endif 	/* __IAudioSessionManager_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "propidl.h"
#include "AudioSessionTypes.h"
#include "AudioClient.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_audiopolicy_0000_0000 */
/* [local] */ 

typedef 
enum AudioSessionDisconnectReason
    {	DisconnectReasonDeviceRemoval	= 0,
	DisconnectReasonServerShutdown	= ( DisconnectReasonDeviceRemoval + 1 ) ,
	DisconnectReasonFormatChanged	= ( DisconnectReasonServerShutdown + 1 ) ,
	DisconnectReasonSessionLogoff	= ( DisconnectReasonFormatChanged + 1 ) ,
	DisconnectReasonSessionDisconnected	= ( DisconnectReasonSessionLogoff + 1 ) ,
	DisconnectReasonExclusiveModeOverride	= ( DisconnectReasonSessionDisconnected + 1 ) 
    } 	AudioSessionDisconnectReason;



extern RPC_IF_HANDLE __MIDL_itf_audiopolicy_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_audiopolicy_0000_0000_v0_0_s_ifspec;

#ifndef __IAudioSessionEvents_INTERFACE_DEFINED__
#define __IAudioSessionEvents_INTERFACE_DEFINED__

/* interface IAudioSessionEvents */
/* [local][uuid][unique][object] */ 


EXTERN_C const IID IID_IAudioSessionEvents;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("24918ACC-64B3-37C1-8CA9-74A66E9957A8")
    IAudioSessionEvents : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnDisplayNameChanged( 
            /* [string][in] */ 
            __in  LPCWSTR NewDisplayName,
            /* [in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnIconPathChanged( 
            /* [string][in] */ 
            __in  LPCWSTR NewIconPath,
            /* [in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnSimpleVolumeChanged( 
            /* [in] */ 
            __in  float NewVolume,
            /* [in] */ 
            __in  BOOL NewMute,
            /* [in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnChannelVolumeChanged( 
            /* [in] */ 
            __in  DWORD ChannelCount,
            /* [size_is][in] */ 
            __in_ecount(ChannelCount)  float NewChannelVolumeArray[  ],
            /* [in] */ 
            __in  DWORD ChangedChannel,
            /* [in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnGroupingParamChanged( 
            /* [in] */ 
            __in  LPCGUID NewGroupingParam,
            /* [in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnStateChanged( 
            /* [in] */ 
            __in  AudioSessionState NewState) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnSessionDisconnected( 
            /* [in] */ 
            __in  AudioSessionDisconnectReason DisconnectReason) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAudioSessionEventsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAudioSessionEvents * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAudioSessionEvents * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAudioSessionEvents * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnDisplayNameChanged )( 
            IAudioSessionEvents * This,
            /* [string][in] */ 
            __in  LPCWSTR NewDisplayName,
            /* [in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *OnIconPathChanged )( 
            IAudioSessionEvents * This,
            /* [string][in] */ 
            __in  LPCWSTR NewIconPath,
            /* [in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *OnSimpleVolumeChanged )( 
            IAudioSessionEvents * This,
            /* [in] */ 
            __in  float NewVolume,
            /* [in] */ 
            __in  BOOL NewMute,
            /* [in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *OnChannelVolumeChanged )( 
            IAudioSessionEvents * This,
            /* [in] */ 
            __in  DWORD ChannelCount,
            /* [size_is][in] */ 
            __in_ecount(ChannelCount)  float NewChannelVolumeArray[  ],
            /* [in] */ 
            __in  DWORD ChangedChannel,
            /* [in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *OnGroupingParamChanged )( 
            IAudioSessionEvents * This,
            /* [in] */ 
            __in  LPCGUID NewGroupingParam,
            /* [in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *OnStateChanged )( 
            IAudioSessionEvents * This,
            /* [in] */ 
            __in  AudioSessionState NewState);
        
        HRESULT ( STDMETHODCALLTYPE *OnSessionDisconnected )( 
            IAudioSessionEvents * This,
            /* [in] */ 
            __in  AudioSessionDisconnectReason DisconnectReason);
        
        END_INTERFACE
    } IAudioSessionEventsVtbl;

    interface IAudioSessionEvents
    {
        CONST_VTBL struct IAudioSessionEventsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAudioSessionEvents_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAudioSessionEvents_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAudioSessionEvents_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAudioSessionEvents_OnDisplayNameChanged(This,NewDisplayName,EventContext)	\
    ( (This)->lpVtbl -> OnDisplayNameChanged(This,NewDisplayName,EventContext) ) 

#define IAudioSessionEvents_OnIconPathChanged(This,NewIconPath,EventContext)	\
    ( (This)->lpVtbl -> OnIconPathChanged(This,NewIconPath,EventContext) ) 

#define IAudioSessionEvents_OnSimpleVolumeChanged(This,NewVolume,NewMute,EventContext)	\
    ( (This)->lpVtbl -> OnSimpleVolumeChanged(This,NewVolume,NewMute,EventContext) ) 

#define IAudioSessionEvents_OnChannelVolumeChanged(This,ChannelCount,NewChannelVolumeArray,ChangedChannel,EventContext)	\
    ( (This)->lpVtbl -> OnChannelVolumeChanged(This,ChannelCount,NewChannelVolumeArray,ChangedChannel,EventContext) ) 

#define IAudioSessionEvents_OnGroupingParamChanged(This,NewGroupingParam,EventContext)	\
    ( (This)->lpVtbl -> OnGroupingParamChanged(This,NewGroupingParam,EventContext) ) 

#define IAudioSessionEvents_OnStateChanged(This,NewState)	\
    ( (This)->lpVtbl -> OnStateChanged(This,NewState) ) 

#define IAudioSessionEvents_OnSessionDisconnected(This,DisconnectReason)	\
    ( (This)->lpVtbl -> OnSessionDisconnected(This,DisconnectReason) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAudioSessionEvents_INTERFACE_DEFINED__ */


#ifndef __IAudioSessionControl_INTERFACE_DEFINED__
#define __IAudioSessionControl_INTERFACE_DEFINED__

/* interface IAudioSessionControl */
/* [local][unique][helpstring][uuid][object] */ 


EXTERN_C const IID IID_IAudioSessionControl;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")
    IAudioSessionControl : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetState( 
            /* [out] */ 
            __out  AudioSessionState *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDisplayName( 
            /* [string][out] */ 
            __out  LPWSTR *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetDisplayName( 
            /* [string][in] */ 
            __in  LPCWSTR Value,
            /* [unique][in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIconPath( 
            /* [string][out] */ 
            __out  LPWSTR *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetIconPath( 
            /* [string][in] */ 
            __in  LPCWSTR Value,
            /* [unique][in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGroupingParam( 
            /* [out] */ 
            __out  GUID *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetGroupingParam( 
            /* [in] */ 
            __in  LPCGUID Override,
            /* [unique][in] */ LPCGUID EventContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RegisterAudioSessionNotification( 
            /* [in] */ 
            __in  IAudioSessionEvents *NewNotifications) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UnregisterAudioSessionNotification( 
            /* [in] */ 
            __in  IAudioSessionEvents *NewNotifications) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAudioSessionControlVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAudioSessionControl * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAudioSessionControl * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAudioSessionControl * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetState )( 
            IAudioSessionControl * This,
            /* [out] */ 
            __out  AudioSessionState *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetDisplayName )( 
            IAudioSessionControl * This,
            /* [string][out] */ 
            __out  LPWSTR *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetDisplayName )( 
            IAudioSessionControl * This,
            /* [string][in] */ 
            __in  LPCWSTR Value,
            /* [unique][in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetIconPath )( 
            IAudioSessionControl * This,
            /* [string][out] */ 
            __out  LPWSTR *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetIconPath )( 
            IAudioSessionControl * This,
            /* [string][in] */ 
            __in  LPCWSTR Value,
            /* [unique][in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetGroupingParam )( 
            IAudioSessionControl * This,
            /* [out] */ 
            __out  GUID *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetGroupingParam )( 
            IAudioSessionControl * This,
            /* [in] */ 
            __in  LPCGUID Override,
            /* [unique][in] */ LPCGUID EventContext);
        
        HRESULT ( STDMETHODCALLTYPE *RegisterAudioSessionNotification )( 
            IAudioSessionControl * This,
            /* [in] */ 
            __in  IAudioSessionEvents *NewNotifications);
        
        HRESULT ( STDMETHODCALLTYPE *UnregisterAudioSessionNotification )( 
            IAudioSessionControl * This,
            /* [in] */ 
            __in  IAudioSessionEvents *NewNotifications);
        
        END_INTERFACE
    } IAudioSessionControlVtbl;

    interface IAudioSessionControl
    {
        CONST_VTBL struct IAudioSessionControlVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAudioSessionControl_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAudioSessionControl_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAudioSessionControl_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAudioSessionControl_GetState(This,pRetVal)	\
    ( (This)->lpVtbl -> GetState(This,pRetVal) ) 

#define IAudioSessionControl_GetDisplayName(This,pRetVal)	\
    ( (This)->lpVtbl -> GetDisplayName(This,pRetVal) ) 

#define IAudioSessionControl_SetDisplayName(This,Value,EventContext)	\
    ( (This)->lpVtbl -> SetDisplayName(This,Value,EventContext) ) 

#define IAudioSessionControl_GetIconPath(This,pRetVal)	\
    ( (This)->lpVtbl -> GetIconPath(This,pRetVal) ) 

#define IAudioSessionControl_SetIconPath(This,Value,EventContext)	\
    ( (This)->lpVtbl -> SetIconPath(This,Value,EventContext) ) 

#define IAudioSessionControl_GetGroupingParam(This,pRetVal)	\
    ( (This)->lpVtbl -> GetGroupingParam(This,pRetVal) ) 

#define IAudioSessionControl_SetGroupingParam(This,Override,EventContext)	\
    ( (This)->lpVtbl -> SetGroupingParam(This,Override,EventContext) ) 

#define IAudioSessionControl_RegisterAudioSessionNotification(This,NewNotifications)	\
    ( (This)->lpVtbl -> RegisterAudioSessionNotification(This,NewNotifications) ) 

#define IAudioSessionControl_UnregisterAudioSessionNotification(This,NewNotifications)	\
    ( (This)->lpVtbl -> UnregisterAudioSessionNotification(This,NewNotifications) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAudioSessionControl_INTERFACE_DEFINED__ */


#ifndef __IAudioSessionManager_INTERFACE_DEFINED__
#define __IAudioSessionManager_INTERFACE_DEFINED__

/* interface IAudioSessionManager */
/* [local][unique][helpstring][uuid][object] */ 


EXTERN_C const IID IID_IAudioSessionManager;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("BFA971F1-4D5E-40BB-935E-967039BFBEE4")
    IAudioSessionManager : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAudioSessionControl( 
            /* [in] */ 
            __in_opt  LPCGUID AudioSessionGuid,
            /* [in] */ 
            __in  DWORD StreamFlags,
            /* [out] */ 
            __deref_out  IAudioSessionControl **SessionControl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSimpleAudioVolume( 
            /* [in] */ 
            __in_opt  LPCGUID AudioSessionGuid,
            /* [in] */ 
            __in  DWORD StreamFlags,
            /* [out] */ 
            __deref_out  ISimpleAudioVolume **AudioVolume) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAudioSessionManagerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAudioSessionManager * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAudioSessionManager * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAudioSessionManager * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAudioSessionControl )( 
            IAudioSessionManager * This,
            /* [in] */ 
            __in_opt  LPCGUID AudioSessionGuid,
            /* [in] */ 
            __in  DWORD StreamFlags,
            /* [out] */ 
            __deref_out  IAudioSessionControl **SessionControl);
        
        HRESULT ( STDMETHODCALLTYPE *GetSimpleAudioVolume )( 
            IAudioSessionManager * This,
            /* [in] */ 
            __in_opt  LPCGUID AudioSessionGuid,
            /* [in] */ 
            __in  DWORD StreamFlags,
            /* [out] */ 
            __deref_out  ISimpleAudioVolume **AudioVolume);
        
        END_INTERFACE
    } IAudioSessionManagerVtbl;

    interface IAudioSessionManager
    {
        CONST_VTBL struct IAudioSessionManagerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAudioSessionManager_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAudioSessionManager_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAudioSessionManager_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAudioSessionManager_GetAudioSessionControl(This,AudioSessionGuid,StreamFlags,SessionControl)	\
    ( (This)->lpVtbl -> GetAudioSessionControl(This,AudioSessionGuid,StreamFlags,SessionControl) ) 

#define IAudioSessionManager_GetSimpleAudioVolume(This,AudioSessionGuid,StreamFlags,AudioVolume)	\
    ( (This)->lpVtbl -> GetSimpleAudioVolume(This,AudioSessionGuid,StreamFlags,AudioVolume) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAudioSessionManager_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif



