/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

#pragma warning disable IDE0055 // We want the values to have a consistent view.

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioChannelLayoutTag Constants <br />
/// These constants are used in the mChannelLayoutTag field of an AudioChannelLayout structure.
/// </summary>
internal enum AudioChannelLayoutTag : uint
{
    // Some channel abbreviations used below:
    // L - left
    // R - right
    // C - center
    // Ls - left surround
    // Rs - right surround
    // Cs - center surround
    // Rls - rear left surround
    // Rrs - rear right surround
    // Lw - left wide
    // Rw - right wide
    // Lsd - left surround direct
    // Rsd - right surround direct
    // Lc - left center
    // Rc - right center
    // Ts - top surround
    // Vhl - vertical height left
    // Vhc - vertical height center
    // Vhr - vertical height right
    // Ltm - left top middle
    // Rtm - right top middle
    // Ltr - left top rear
    // Ctr - center top rear
    // Rtr - right top rear
    // Lss - left side surround
    // Rss - right side surround
    // Lb - left bottom
    // Rb - Right bottom
    // Cb - Center bottom
    // Lts - Left top surround
    // Rts - Right top surround
    // Leos - Left edge of screen
    // Reos - Right edge of screen
    // Lbs - Left back surround
    // Rbs - Right back surround
    // Lt - left matrix total. for matrix encoded stereo.
    // Rt - right matrix total. for matrix encoded stereo.

    //  General layouts
    kAudioChannelLayoutTag_UseChannelDescriptions   = (0U<<16) | 0,     // < use the array of AudioChannelDescriptions to define the mapping.
    kAudioChannelLayoutTag_UseChannelBitmap         = (1U<<16) | 0,     // < use the bitmap to define the mapping.

    kAudioChannelLayoutTag_Mono                     = (100U<<16) | 1,   // < a standard mono stream
    kAudioChannelLayoutTag_Stereo                   = (101U<<16) | 2,   // < a standard stereo stream (L R) - implied playback
    kAudioChannelLayoutTag_StereoHeadphones         = (102U<<16) | 2,   // < a standard stereo stream (L R) - implied headphone playback
    kAudioChannelLayoutTag_MatrixStereo             = (103U<<16) | 2,   // < a matrix encoded stereo stream (Lt, Rt)
    kAudioChannelLayoutTag_MidSide                  = (104U<<16) | 2,   // < mid/side recording
    kAudioChannelLayoutTag_XY                       = (105U<<16) | 2,   // < coincident mic pair (often 2 figure 8's)
    kAudioChannelLayoutTag_Binaural                 = (106U<<16) | 2,   // < binaural stereo (left, right)
    kAudioChannelLayoutTag_Ambisonic_B_Format       = (107U<<16) | 4,   // < W, X, Y, Z

    kAudioChannelLayoutTag_Quadraphonic             = (108U<<16) | 4,   // < L R Ls Rs  -- 90 degree speaker separation
    kAudioChannelLayoutTag_Pentagonal               = (109U<<16) | 5,   // < L R Ls Rs C  -- 72 degree speaker separation
    kAudioChannelLayoutTag_Hexagonal                = (110U<<16) | 6,   // < L R Ls Rs C Cs  -- 60 degree speaker separation
    kAudioChannelLayoutTag_Octagonal                = (111U<<16) | 8,   // < L R Ls Rs C Cs Lw Rw  -- 45 degree speaker separation
    kAudioChannelLayoutTag_Cube                     = (112U<<16) | 8,   // < left, right, rear left, rear right
                                                                        // < top left, top right, top rear left, top rear right

    //  MPEG defined layouts
    kAudioChannelLayoutTag_MPEG_1_0                 = kAudioChannelLayoutTag_Mono,          // <  C
    kAudioChannelLayoutTag_MPEG_2_0                 = kAudioChannelLayoutTag_Stereo,        // <  L R
    kAudioChannelLayoutTag_MPEG_3_0_A               = (113U<<16) | 3,                       // <  L R C
    kAudioChannelLayoutTag_MPEG_3_0_B               = (114U<<16) | 3,                       // <  C L R
    kAudioChannelLayoutTag_MPEG_4_0_A               = (115U<<16) | 4,                       // <  L R C Cs
    kAudioChannelLayoutTag_MPEG_4_0_B               = (116U<<16) | 4,                       // <  C L R Cs
    kAudioChannelLayoutTag_MPEG_5_0_A               = (117U<<16) | 5,                       // <  L R C Ls Rs
    kAudioChannelLayoutTag_MPEG_5_0_B               = (118U<<16) | 5,                       // <  L R Ls Rs C
    kAudioChannelLayoutTag_MPEG_5_0_C               = (119U<<16) | 5,                       // <  L C R Ls Rs
    kAudioChannelLayoutTag_MPEG_5_0_D               = (120U<<16) | 5,                       // <  C L R Ls Rs
    kAudioChannelLayoutTag_MPEG_5_1_A               = (121U<<16) | 6,                       // <  L R C LFE Ls Rs
    kAudioChannelLayoutTag_MPEG_5_1_B               = (122U<<16) | 6,                       // <  L R Ls Rs C LFE
    kAudioChannelLayoutTag_MPEG_5_1_C               = (123U<<16) | 6,                       // <  L C R Ls Rs LFE
    kAudioChannelLayoutTag_MPEG_5_1_D               = (124U<<16) | 6,                       // <  C L R Ls Rs LFE
    kAudioChannelLayoutTag_MPEG_6_1_A               = (125U<<16) | 7,                       // <  L R C LFE Ls Rs Cs
    kAudioChannelLayoutTag_MPEG_7_1_A               = (126U<<16) | 8,                       // <  L R C LFE Ls Rs Lc Rc
    kAudioChannelLayoutTag_MPEG_7_1_B               = (127U<<16) | 8,                       // <  C Lc Rc L R Ls Rs LFE    (doc: IS-13818-7 MPEG2-AAC Table 3.1)
    kAudioChannelLayoutTag_MPEG_7_1_C               = (128U<<16) | 8,                       // <  L R C LFE Ls Rs Rls Rrs
    kAudioChannelLayoutTag_Emagic_Default_7_1       = (129U<<16) | 8,                       // <  L R Ls Rs C LFE Lc Rc
    kAudioChannelLayoutTag_SMPTE_DTV                = (130U<<16) | 8,                       // <  L R C LFE Ls Rs Lt Rt
                                                                                            // <      (kAudioChannelLayoutTag_ITU_5_1 plus a matrix encoded stereo mix)

    //  ITU defined layouts
    kAudioChannelLayoutTag_ITU_1_0                  = kAudioChannelLayoutTag_Mono,          // <  C
    kAudioChannelLayoutTag_ITU_2_0                  = kAudioChannelLayoutTag_Stereo,        // <  L R

    kAudioChannelLayoutTag_ITU_2_1                  = (131U<<16) | 3,                       // <  L R Cs
    kAudioChannelLayoutTag_ITU_2_2                  = (132U<<16) | 4,                       // <  L R Ls Rs
    kAudioChannelLayoutTag_ITU_3_0                  = kAudioChannelLayoutTag_MPEG_3_0_A,    // <  L R C
    kAudioChannelLayoutTag_ITU_3_1                  = kAudioChannelLayoutTag_MPEG_4_0_A,    // <  L R C Cs

    kAudioChannelLayoutTag_ITU_3_2                  = kAudioChannelLayoutTag_MPEG_5_0_A,    // <  L R C Ls Rs
    kAudioChannelLayoutTag_ITU_3_2_1                = kAudioChannelLayoutTag_MPEG_5_1_A,    // <  L R C LFE Ls Rs
    kAudioChannelLayoutTag_ITU_3_4_1                = kAudioChannelLayoutTag_MPEG_7_1_C,    // <  L R C LFE Ls Rs Rls Rrs

    // DVD defined layouts
    kAudioChannelLayoutTag_DVD_0                    = kAudioChannelLayoutTag_Mono,          // < C (mono)
    kAudioChannelLayoutTag_DVD_1                    = kAudioChannelLayoutTag_Stereo,        // < L R
    kAudioChannelLayoutTag_DVD_2                    = kAudioChannelLayoutTag_ITU_2_1,       // < L R Cs
    kAudioChannelLayoutTag_DVD_3                    = kAudioChannelLayoutTag_ITU_2_2,       // < L R Ls Rs
    kAudioChannelLayoutTag_DVD_4                    = (133U<<16) | 3,                       // < L R LFE
    kAudioChannelLayoutTag_DVD_5                    = (134U<<16) | 4,                       // < L R LFE Cs
    kAudioChannelLayoutTag_DVD_6                    = (135U<<16) | 5,                       // < L R LFE Ls Rs
    kAudioChannelLayoutTag_DVD_7                    = kAudioChannelLayoutTag_MPEG_3_0_A,    // < L R C
    kAudioChannelLayoutTag_DVD_8                    = kAudioChannelLayoutTag_MPEG_4_0_A,    // < L R C Cs
    kAudioChannelLayoutTag_DVD_9                    = kAudioChannelLayoutTag_MPEG_5_0_A,    // < L R C Ls Rs
    kAudioChannelLayoutTag_DVD_10                   = (136U<<16) | 4,                       // < L R C LFE
    kAudioChannelLayoutTag_DVD_11                   = (137U<<16) | 5,                       // < L R C LFE Cs
    kAudioChannelLayoutTag_DVD_12                   = kAudioChannelLayoutTag_MPEG_5_1_A,    // < L R C LFE Ls Rs
    // 13 through 17 are duplicates of 8 through 12.
    kAudioChannelLayoutTag_DVD_13                   = kAudioChannelLayoutTag_DVD_8,         // < L R C Cs
    kAudioChannelLayoutTag_DVD_14                   = kAudioChannelLayoutTag_DVD_9,         // < L R C Ls Rs
    kAudioChannelLayoutTag_DVD_15                   = kAudioChannelLayoutTag_DVD_10,        // < L R C LFE
    kAudioChannelLayoutTag_DVD_16                   = kAudioChannelLayoutTag_DVD_11,        // < L R C LFE Cs
    kAudioChannelLayoutTag_DVD_17                   = kAudioChannelLayoutTag_DVD_12,        // < L R C LFE Ls Rs
    kAudioChannelLayoutTag_DVD_18                   = (138U<<16) | 5,                       // < L R Ls Rs LFE
    kAudioChannelLayoutTag_DVD_19                   = kAudioChannelLayoutTag_MPEG_5_0_B,    // < L R Ls Rs C
    kAudioChannelLayoutTag_DVD_20                   = kAudioChannelLayoutTag_MPEG_5_1_B,    // < L R Ls Rs C LFE

    // These layouts are recommended for AudioUnit usage
        // These are the symmetrical layouts
    kAudioChannelLayoutTag_AudioUnit_4              = kAudioChannelLayoutTag_Quadraphonic,
    kAudioChannelLayoutTag_AudioUnit_5              = kAudioChannelLayoutTag_Pentagonal,
    kAudioChannelLayoutTag_AudioUnit_6              = kAudioChannelLayoutTag_Hexagonal,
    kAudioChannelLayoutTag_AudioUnit_8              = kAudioChannelLayoutTag_Octagonal,
        // These are the surround-based layouts
    kAudioChannelLayoutTag_AudioUnit_5_0            = kAudioChannelLayoutTag_MPEG_5_0_B,    // < L R Ls Rs C
    kAudioChannelLayoutTag_AudioUnit_6_0            = (139U<<16) | 6,                       // < L R Ls Rs C Cs
    kAudioChannelLayoutTag_AudioUnit_7_0            = (140U<<16) | 7,                       // < L R Ls Rs C Rls Rrs
    kAudioChannelLayoutTag_AudioUnit_7_0_Front      = (148U<<16) | 7,                       // < L R Ls Rs C Lc Rc
    kAudioChannelLayoutTag_AudioUnit_5_1            = kAudioChannelLayoutTag_MPEG_5_1_A,    // < L R C LFE Ls Rs
    kAudioChannelLayoutTag_AudioUnit_6_1            = kAudioChannelLayoutTag_MPEG_6_1_A,    // < L R C LFE Ls Rs Cs
    kAudioChannelLayoutTag_AudioUnit_7_1            = kAudioChannelLayoutTag_MPEG_7_1_C,    // < L R C LFE Ls Rs Rls Rrs
    kAudioChannelLayoutTag_AudioUnit_7_1_Front      = kAudioChannelLayoutTag_MPEG_7_1_A,    // < L R C LFE Ls Rs Lc Rc

    kAudioChannelLayoutTag_AAC_3_0                  = kAudioChannelLayoutTag_MPEG_3_0_B,    // < C L R
    kAudioChannelLayoutTag_AAC_Quadraphonic         = kAudioChannelLayoutTag_Quadraphonic,  // < L R Ls Rs
    kAudioChannelLayoutTag_AAC_4_0                  = kAudioChannelLayoutTag_MPEG_4_0_B,    // < C L R Cs
    kAudioChannelLayoutTag_AAC_5_0                  = kAudioChannelLayoutTag_MPEG_5_0_D,    // < C L R Ls Rs
    kAudioChannelLayoutTag_AAC_5_1                  = kAudioChannelLayoutTag_MPEG_5_1_D,    // < C L R Ls Rs Lfe
    kAudioChannelLayoutTag_AAC_6_0                  = (141U<<16) | 6,                       // < C L R Ls Rs Cs
    kAudioChannelLayoutTag_AAC_6_1                  = (142U<<16) | 7,                       // < C L R Ls Rs Cs Lfe
    kAudioChannelLayoutTag_AAC_7_0                  = (143U<<16) | 7,                       // < C L R Ls Rs Rls Rrs
    kAudioChannelLayoutTag_AAC_7_1                  = kAudioChannelLayoutTag_MPEG_7_1_B,    // < C Lc Rc L R Ls Rs Lfe
    kAudioChannelLayoutTag_AAC_7_1_B                = (183U<<16) | 8,                       // < C L R Ls Rs Rls Rrs LFE
    kAudioChannelLayoutTag_AAC_7_1_C                = (184U<<16) | 8,                       // < C L R Ls Rs LFE Vhl Vhr
    kAudioChannelLayoutTag_AAC_Octagonal            = (144U<<16) | 8,                       // < C L R Ls Rs Rls Rrs Cs

    kAudioChannelLayoutTag_TMH_10_2_std             = (145U<<16) | 16,                      // < L R C Vhc Lsd Rsd Ls Rs Vhl Vhr Lw Rw Csd Cs LFE1 LFE2
    kAudioChannelLayoutTag_TMH_10_2_full            = (146U<<16) | 21,                      // < TMH_10_2_std plus: Lc Rc HI VI Haptic

    kAudioChannelLayoutTag_AC3_1_0_1                = (149U<<16) | 2,                       // < C LFE
    kAudioChannelLayoutTag_AC3_3_0                  = (150U<<16) | 3,                       // < L C R
    kAudioChannelLayoutTag_AC3_3_1                  = (151U<<16) | 4,                       // < L C R Cs
    kAudioChannelLayoutTag_AC3_3_0_1                = (152U<<16) | 4,                       // < L C R LFE
    kAudioChannelLayoutTag_AC3_2_1_1                = (153U<<16) | 4,                       // < L R Cs LFE
    kAudioChannelLayoutTag_AC3_3_1_1                = (154U<<16) | 5,                       // < L C R Cs LFE

    kAudioChannelLayoutTag_EAC_6_0_A                = (155U<<16) | 6,                       // < L C R Ls Rs Cs
    kAudioChannelLayoutTag_EAC_7_0_A                = (156U<<16) | 7,                       // < L C R Ls Rs Rls Rrs

    kAudioChannelLayoutTag_EAC3_6_1_A               = (157U<<16) | 7,                       // < L C R Ls Rs LFE Cs
    kAudioChannelLayoutTag_EAC3_6_1_B               = (158U<<16) | 7,                       // < L C R Ls Rs LFE Ts
    kAudioChannelLayoutTag_EAC3_6_1_C               = (159U<<16) | 7,                       // < L C R Ls Rs LFE Vhc
    kAudioChannelLayoutTag_EAC3_7_1_A               = (160U<<16) | 8,                       // < L C R Ls Rs LFE Rls Rrs
    kAudioChannelLayoutTag_EAC3_7_1_B               = (161U<<16) | 8,                       // < L C R Ls Rs LFE Lc Rc
    kAudioChannelLayoutTag_EAC3_7_1_C               = (162U<<16) | 8,                       // < L C R Ls Rs LFE Lsd Rsd
    kAudioChannelLayoutTag_EAC3_7_1_D               = (163U<<16) | 8,                       // < L C R Ls Rs LFE Lw Rw
    kAudioChannelLayoutTag_EAC3_7_1_E               = (164U<<16) | 8,                       // < L C R Ls Rs LFE Vhl Vhr

    kAudioChannelLayoutTag_EAC3_7_1_F               = (165U<<16) | 8,                       // < L C R Ls Rs LFE Cs Ts
    kAudioChannelLayoutTag_EAC3_7_1_G               = (166U<<16) | 8,                       // < L C R Ls Rs LFE Cs Vhc
    kAudioChannelLayoutTag_EAC3_7_1_H               = (167U<<16) | 8,                       // < L C R Ls Rs LFE Ts Vhc

    kAudioChannelLayoutTag_DTS_3_1                  = (168U<<16) | 4,                       // < C L R LFE
    kAudioChannelLayoutTag_DTS_4_1                  = (169U<<16) | 5,                       // < C L R Cs LFE
    kAudioChannelLayoutTag_DTS_6_0_A                = (170U<<16) | 6,                       // < Lc Rc L R Ls Rs
    kAudioChannelLayoutTag_DTS_6_0_B                = (171U<<16) | 6,                       // < C L R Rls Rrs Ts
    kAudioChannelLayoutTag_DTS_6_0_C                = (172U<<16) | 6,                       // < C Cs L R Rls Rrs
    kAudioChannelLayoutTag_DTS_6_1_A                = (173U<<16) | 7,                       // < Lc Rc L R Ls Rs LFE
    kAudioChannelLayoutTag_DTS_6_1_B                = (174U<<16) | 7,                       // < C L R Rls Rrs Ts LFE
    kAudioChannelLayoutTag_DTS_6_1_C                = (175U<<16) | 7,                       // < C Cs L R Rls Rrs LFE
    kAudioChannelLayoutTag_DTS_7_0                  = (176U<<16) | 7,                       // < Lc C Rc L R Ls Rs
    kAudioChannelLayoutTag_DTS_7_1                  = (177U<<16) | 8,                       // < Lc C Rc L R Ls Rs LFE
    kAudioChannelLayoutTag_DTS_8_0_A                = (178U<<16) | 8,                       // < Lc Rc L R Ls Rs Rls Rrs
    kAudioChannelLayoutTag_DTS_8_0_B                = (179U<<16) | 8,                       // < Lc C Rc L R Ls Cs Rs
    kAudioChannelLayoutTag_DTS_8_1_A                = (180U<<16) | 9,                       // < Lc Rc L R Ls Rs Rls Rrs LFE
    kAudioChannelLayoutTag_DTS_8_1_B                = (181U<<16) | 9,                       // < Lc C Rc L R Ls Cs Rs LFE
    kAudioChannelLayoutTag_DTS_6_1_D                = (182U<<16) | 7,                       // < C L R Ls Rs LFE Cs

    kAudioChannelLayoutTag_WAVE_2_1                 = kAudioChannelLayoutTag_DVD_4,         // < 3 channels, L R LFE
    kAudioChannelLayoutTag_WAVE_3_0                 = kAudioChannelLayoutTag_MPEG_3_0_A,    // < 3 channels, L R C
    kAudioChannelLayoutTag_WAVE_4_0_A               = kAudioChannelLayoutTag_ITU_2_2,       // < 4 channels, L R Ls Rs
    kAudioChannelLayoutTag_WAVE_4_0_B               = (185U<<16) | 4,                       // < 4 channels, L R Rls Rrs
    kAudioChannelLayoutTag_WAVE_5_0_A               = kAudioChannelLayoutTag_MPEG_5_0_A,    // < 5 channels, L R C Ls Rs
    kAudioChannelLayoutTag_WAVE_5_0_B               = (186U<<16) | 5,                       // < 5 channels, L R C Rls Rrs
    kAudioChannelLayoutTag_WAVE_5_1_A               = kAudioChannelLayoutTag_MPEG_5_1_A,    // < 6 channels, L R C LFE Ls Rs
    kAudioChannelLayoutTag_WAVE_5_1_B               = (187U<<16) | 6,                       // < 6 channels, L R C LFE Rls Rrs
    kAudioChannelLayoutTag_WAVE_6_1                 = (188U<<16) | 7,                       // < 7 channels, L R C LFE Cs Ls Rs
    kAudioChannelLayoutTag_WAVE_7_1                 = (189U<<16) | 8,                       // < 8 channels, L R C LFE Rls Rrs Ls Rs

    kAudioChannelLayoutTag_HOA_ACN_SN3D             = (190U<<16) | 0,                       // < Higher Order Ambisonics, Ambisonics Channel Number, SN3D normalization
                                                                                            // < needs to be ORed with the actual number of channels (not the HOA order)
    kAudioChannelLayoutTag_HOA_ACN_N3D              = (191U<<16) | 0,                       // < Higher Order Ambisonics, Ambisonics Channel Number, N3D normalization
                                                                                            // < needs to be ORed with the actual number of channels (not the HOA order)
    
    kAudioChannelLayoutTag_Atmos_5_1_2              = (194U<<16) | 8,                       // < L R C LFE Ls Rs Ltm Rtm
    kAudioChannelLayoutTag_Atmos_5_1_4              = (195U<<16) | 10,                      // < L R C LFE Ls Rs Vhl Vhr Ltr Rtr
    kAudioChannelLayoutTag_Atmos_7_1_2              = (196U<<16) | 10,                      // < L R C LFE Ls Rs Rls Rrs Ltm Rtm
    kAudioChannelLayoutTag_Atmos_7_1_4              = (192U<<16) | 12,                      // < L R C LFE Ls Rs Rls Rrs Vhl Vhr Ltr Rtr
    kAudioChannelLayoutTag_Atmos_9_1_6              = (193U<<16) | 16,                      // < L R C LFE Ls Rs Rls Rrs Lw Rw Vhl Vhr Ltm Rtm Ltr Rtr

    kAudioChannelLayoutTag_Logic_Mono               = kAudioChannelLayoutTag_Mono,          // < C
    kAudioChannelLayoutTag_Logic_Stereo             = kAudioChannelLayoutTag_Stereo,        // < L R
    kAudioChannelLayoutTag_Logic_Quadraphonic       = kAudioChannelLayoutTag_Quadraphonic,  // < L R Ls Rs
    kAudioChannelLayoutTag_Logic_4_0_A              = kAudioChannelLayoutTag_MPEG_4_0_A,    // < L R C Cs
    kAudioChannelLayoutTag_Logic_4_0_B              = kAudioChannelLayoutTag_MPEG_4_0_B,    // < C L R Cs
    kAudioChannelLayoutTag_Logic_4_0_C              = (197U<<16) | 4,                       // < L R Cs C
    kAudioChannelLayoutTag_Logic_5_0_A              = kAudioChannelLayoutTag_MPEG_5_0_A,    // < L R C Ls Rs
    kAudioChannelLayoutTag_Logic_5_0_B              = kAudioChannelLayoutTag_MPEG_5_0_B,    // < L R Ls Rs C
    kAudioChannelLayoutTag_Logic_5_0_C              = kAudioChannelLayoutTag_MPEG_5_0_C,    // < L C R Ls Rs
    kAudioChannelLayoutTag_Logic_5_0_D              = kAudioChannelLayoutTag_MPEG_5_0_D,    // < C L R Ls Rs
    kAudioChannelLayoutTag_Logic_5_1_A              = kAudioChannelLayoutTag_MPEG_5_1_A,    // < L R C LFE Ls Rs
    kAudioChannelLayoutTag_Logic_5_1_B              = kAudioChannelLayoutTag_MPEG_5_1_B,    // < L R Ls Rs C LFE
    kAudioChannelLayoutTag_Logic_5_1_C              = kAudioChannelLayoutTag_MPEG_5_1_C,    // < L C R Ls Rs LFE
    kAudioChannelLayoutTag_Logic_5_1_D              = kAudioChannelLayoutTag_MPEG_5_1_D,    // < C L R Ls Rs LFE
    kAudioChannelLayoutTag_Logic_6_0_A              = kAudioChannelLayoutTag_AAC_6_0,       // < C L R Ls Rs Cs
    kAudioChannelLayoutTag_Logic_6_0_B              = (198U<<16) | 6,                       // < L R Ls Rs Cs C
    kAudioChannelLayoutTag_Logic_6_0_C              = kAudioChannelLayoutTag_AudioUnit_6_0, // < L R Ls Rs C Cs
    kAudioChannelLayoutTag_Logic_6_1_A              = kAudioChannelLayoutTag_AAC_6_1,       // < C L R Ls Rs Cs LFE
    kAudioChannelLayoutTag_Logic_6_1_B              = (199U<<16) | 7,                       // < L R Ls Rs Cs C LFE
    kAudioChannelLayoutTag_Logic_6_1_C              = kAudioChannelLayoutTag_MPEG_6_1_A,    // < L R C LFE Ls Rs Cs
    kAudioChannelLayoutTag_Logic_6_1_D              = (200U<<16) | 7,                       // < L C R Ls Cs Rs LFE
    kAudioChannelLayoutTag_Logic_7_1_A              = kAudioChannelLayoutTag_AudioUnit_7_1, // < L R C LFE Ls Rs Rls Rrs
    kAudioChannelLayoutTag_Logic_7_1_B              = (201U<<16) | 8,                       // < L R Ls Rs Rls Rrs C LFE
    kAudioChannelLayoutTag_Logic_7_1_C              = kAudioChannelLayoutTag_MPEG_7_1_C,    // < L R C LFE Ls Rs Rls Rrs
    kAudioChannelLayoutTag_Logic_7_1_SDDS_A         = kAudioChannelLayoutTag_MPEG_7_1_A,    // < L R C LFE Ls Rs Lc Rc
    kAudioChannelLayoutTag_Logic_7_1_SDDS_B         = kAudioChannelLayoutTag_MPEG_7_1_B,    // < C Lc Rc L R Ls Rs LFE
    kAudioChannelLayoutTag_Logic_7_1_SDDS_C         = kAudioChannelLayoutTag_Emagic_Default_7_1, // < L R Ls Rs C LFE Lc Rc
    kAudioChannelLayoutTag_Logic_Atmos_5_1_2        = kAudioChannelLayoutTag_Atmos_5_1_2,   // < L R C LFE Ls Rs Ltm Rtm
    kAudioChannelLayoutTag_Logic_Atmos_5_1_4        = kAudioChannelLayoutTag_Atmos_5_1_4,   // < L R C LFE Ls Rs Vhl Vhr Ltr Rtr
    kAudioChannelLayoutTag_Logic_Atmos_7_1_2        = kAudioChannelLayoutTag_Atmos_7_1_2,   // < L R C LFE Ls Rs Rls Rrs Ltm Rtm
    kAudioChannelLayoutTag_Logic_Atmos_7_1_4_A      = kAudioChannelLayoutTag_Atmos_7_1_4,   // < L R C LFE Ls Rs Rls Rrs Vhl Vhr Ltr Rtr
    kAudioChannelLayoutTag_Logic_Atmos_7_1_4_B      = (202U<<16) | 12,                      // < L R Rls Rrs Ls Rs C LFE Vhl Vhr Ltr Rtr
    kAudioChannelLayoutTag_Logic_Atmos_7_1_6        = (203U<<16) | 14,                      // < L R Rls Rrs Ls Rs C LFE Vhl Vhr Ltm Rtm Ltr Rtr

    kAudioChannelLayoutTag_DiscreteInOrder          = (147U<<16) | 0,                       // < needs to be ORed with the actual number of channels
    
    // ISO/IEC 23091-3, channels w/orderings
    kAudioChannelLayoutTag_CICP_1					= kAudioChannelLayoutTag_MPEG_1_0,		// < C
    kAudioChannelLayoutTag_CICP_2 					= kAudioChannelLayoutTag_MPEG_2_0,		// < L R
    kAudioChannelLayoutTag_CICP_3 					= kAudioChannelLayoutTag_MPEG_3_0_A,	// < L R C
    kAudioChannelLayoutTag_CICP_4 					= kAudioChannelLayoutTag_MPEG_4_0_A,	// < L R C Cs
    kAudioChannelLayoutTag_CICP_5					= kAudioChannelLayoutTag_MPEG_5_0_A,	// < L R C Ls Rs
    kAudioChannelLayoutTag_CICP_6					= kAudioChannelLayoutTag_MPEG_5_1_A,	// < L R C LFE Ls Rs
    kAudioChannelLayoutTag_CICP_7					= kAudioChannelLayoutTag_MPEG_7_1_B,	// < L R C LFE Ls Rs Lc Rc

    kAudioChannelLayoutTag_CICP_9					= kAudioChannelLayoutTag_ITU_2_1,		// < L R Cs
    kAudioChannelLayoutTag_CICP_10 					= kAudioChannelLayoutTag_ITU_2_2,		// < L R Ls Rs
    kAudioChannelLayoutTag_CICP_11					= kAudioChannelLayoutTag_MPEG_6_1_A,	// < L R C LFE Ls Rs Cs
    kAudioChannelLayoutTag_CICP_12 					= kAudioChannelLayoutTag_MPEG_7_1_C,	// < L R C LFE Ls Rs Rls Rrs
    kAudioChannelLayoutTag_CICP_13 					= (204U<<16) | 24,						// < Lc Rc C LFE2 Rls Rrs L R Cs LFE3 Lss Rss Vhl Vhr Vhc Ts Ltr Rtr Ltm Rtm Ctr Cb Lb Rb

    kAudioChannelLayoutTag_CICP_14 					= (205U<<16) | 8,						// < L R C LFE Ls Rs Vhl Vhr
    kAudioChannelLayoutTag_CICP_15 					= (206U<<16) | 12,						// < L R C LFE2 Rls Rrs LFE3 Lss Rss Vhl Vhr Ctr

    kAudioChannelLayoutTag_CICP_16 					= (207U<<16) | 10,						// < L R C LFE Ls Rs Vhl Vhr Lts Rts
    kAudioChannelLayoutTag_CICP_17 					= (208U<<16) | 12,						// < L R C LFE Ls Rs Vhl Vhr Vhc Lts Rts Ts
    kAudioChannelLayoutTag_CICP_18 					= (209U<<16) | 14,						// < L R C LFE Ls Rs Lbs Rbs Vhl Vhr Vhc Lts Rts Ts

    kAudioChannelLayoutTag_CICP_19 					= (210U<<16) | 12,						// < L R C LFE Rls Rrs Lss Rss Vhl Vhr Ltr Rtr
    kAudioChannelLayoutTag_CICP_20 					= (211U<<16) | 14,						// < L R C LFE Rls Rrs Lss Rss Vhl Vhr Ltr Rtr Leos Reos

    kAudioChannelLayoutTag_Ogg_3_0                  = kAudioChannelLayoutTag_AC3_3_0,		// < 3 channels, L C R
    kAudioChannelLayoutTag_Ogg_4_0                  = kAudioChannelLayoutTag_WAVE_4_0_B,	// < 4 channels, L R Rls Rrs
    kAudioChannelLayoutTag_Ogg_5_0                  = (212U<<16) | 5,	                    // < 5 channels, L C R Rls Rrs
    kAudioChannelLayoutTag_Ogg_5_1                  = (213U<<16) | 6,	                    // < 6 channels, L C R Rls Rrs LFE
    kAudioChannelLayoutTag_Ogg_6_1                  = (214U<<16) | 7,						// < 7 channels, L C R Ls Rs Cs LFE
    kAudioChannelLayoutTag_Ogg_7_1                  = (215U<<16) | 8,						// < 8 channels, L C R Ls Rs Rls Rrs LFE

    kAudioChannelLayoutTag_MPEG_5_0_E               = (216U<<16) | 5,						// < 5 channels, L R Rls Rrs C
    kAudioChannelLayoutTag_MPEG_5_1_E               = (217U<<16) | 6,						// < 6 channels, L R Rls Rrs C LFE
    kAudioChannelLayoutTag_MPEG_6_1_B               = (218U<<16) | 7,						// < 7 channels, L R Ls Rs C Cs LFE
    kAudioChannelLayoutTag_MPEG_7_1_D               = (219U<<16) | 8,						// < 8 channels, L R Rls Rrs Ls Rs C LFE

    kAudioChannelLayoutTag_BeginReserved            = 0xF0000000,                           // < Channel layout tag values in this range are reserved for internal use
    kAudioChannelLayoutTag_EndReserved              = 0xFFFEFFFF,
    
    kAudioChannelLayoutTag_Unknown                  = 0xFFFF0000                            // < needs to be ORed with the actual number of channels
}