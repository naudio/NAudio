; Audio File Inspector install script
; originally based on example2 that comes with nsis
;
; This script is based on example1.nsi, but it remember the directory, 
; has uninstall support and (optionally) installs start menu shortcuts.
;
; It will install example2.nsi into a directory that the user selects,

;--------------------------------
!include WordFunc.nsh
!insertmacro VersionCompare
 
!include LogicLib.nsh

!define VERSION "0.1"
!define FULLVERSION "0.1.1.0"
!define PRODUCT_NAME "Audio File Inspector"
!define EXECUTABLE_NAME "AudioFileInspector.exe"

; The name of the installer
Name "Audio File Inspector"

; The file to write
;OutFile "${PRODUCT_NAME} v${VERSION} Install.exe"
OutFile "audio_file_inspector_0_1_install.exe"

; The default installation directory
InstallDir "$PROGRAMFILES\Mark Heath\${PRODUCT_NAME}"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"

VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "Comments" ""
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "Mark Heath"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "© 2006-2009 Mark Heath"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "${PRODUCT_NAME} Installer"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${VERSION}"
VIProductVersion "${FULLVERSION}"



;--------------------------------

; Utils

Function .onInit
  Call GetDotNETVersion
  Pop $0
  ${If} $0 == "not found"
    MessageBox MB_OK|MB_ICONSTOP ".NET runtime library v2.0 or newer is required."					
    ExecShell "open" "http://msdn.microsoft.com/netframework/downloads/updates/default.aspx"
    Abort
  ${EndIf}
 
  StrCpy $0 $0 "" 1 # skip "v"
 
  ${VersionCompare} $0 "2.0" $1
  ${If} $1 == 2
    MessageBox MB_OK|MB_ICONSTOP ".NET runtime library v2.0 or newer is required. You have $0."
    ExecShell "open" "http://msdn.microsoft.com/netframework/downloads/updates/default.aspx"
    Abort
  ${EndIf}
FunctionEnd
 
Function GetDotNETVersion
  Push $0
  Push $1
 
  System::Call "mscoree::GetCORVersion(w .r0, i ${NSIS_MAX_STRLEN}, *i) i .r1"
  StrCmp $1 "error" 0 +2
    StrCpy $0 "not found"
 
  Pop $1
  Exch $0
FunctionEnd

;--------------------------------

; Pages

Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "Program Files (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "${EXECUTABLE_NAME}"
  ; File "${EXECUTABLE_NAME}.config"
  File "NAudio.dll"
  File "audio_file_inspector.html"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "SOFTWARE\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
  ExecWait '"$INSTDIR\${EXECUTABLE_NAME}" -install' $0
  DetailPrint "Associating File Types returned $0"
SectionEnd

; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\${EXECUTABLE_NAME}" "" "$INSTDIR\${EXECUTABLE_NAME}" 0
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"  
  ExecWait '"$INSTDIR\${EXECUTABLE_NAME}" -uninstall' $0
  DetailPrint "Removing Explorer Context Action returned $0"
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "SOFTWARE\${PRODUCT_NAME}"

  ; Remove files and uninstaller
  Delete "$INSTDIR\uninstall.exe"
  Delete "$INSTDIR\${EXECUTABLE_NAME}"
  Delete "$INSTDIR\${EXECUTABLE_NAME}.config"
  Delete "$INSTDIR\NAudio.dll"
  Delete "$INSTDIR\audio_file_inspector.html"

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"

  ; Remove directories used
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  RMDir "$INSTDIR"

SectionEnd
