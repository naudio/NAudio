; MIDI File Splitter install script
; originally based on example2 that comes with nsis
;
; This script is based on example1.nsi, but it remember the directory, 
; has uninstall support and (optionally) installs start menu shortcuts.
;
; It will install example2.nsi into a directory that the user selects,

;--------------------------------
!include "MUI.nsh"
!include WordFunc.nsh
!insertmacro VersionCompare
 
!include LogicLib.nsh

!define VERSION "0.5"
!define FULLVERSION "0.5.5.0"
!define PRODUCT_NAME "MIDI File Splitter"
!define EXECUTABLE_NAME "MIDIFileSplitter.exe"
!define HELP_FILE "midi_file_splitter.html"

; The name of the installer
Name "${PRODUCT_NAME}"

; The file to write
; OutFile "${PRODUCT_NAME} v${VERSION} Install.exe"
OutFile "midi_file_splitter_0_5_install.exe"

; The default installation directory
InstallDir "$PROGRAMFILES\Mark Heath\${PRODUCT_NAME}"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"

VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "Comments" ""
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "Mark Heath"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "© 2006 Mark Heath"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "${PRODUCT_NAME} Installer"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${VERSION}"
VIProductVersion "${FULLVERSION}"

Var STARTMENU_FOLDER
Var MUI_TEMP

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


;-- not needed any more !insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY

  ;Start Menu Folder Page Configuration
  !define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU" 
  !define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\Mark Heath\${PRODUCT_NAME}" 
  !define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"

!insertmacro MUI_PAGE_STARTMENU StartMenuPageId $STARTMENU_FOLDER  
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

;--------------------------------

!insertmacro MUI_LANGUAGE "English"

;--------------------------------

; The stuff to install
Section "Program Files (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "${EXECUTABLE_NAME}"
  File "${EXECUTABLE_NAME}.config"
  File "NAudio.dll"
  File "${HELP_FILE}"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "SOFTWARE\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
  
  ; Create the shortcuts
  !insertmacro MUI_STARTMENU_WRITE_BEGIN StartMenuPageId
  CreateDirectory "$SMPROGRAMS\$STARTMENU_FOLDER"
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\${PRODUCT_NAME}.lnk" "$INSTDIR\${EXECUTABLE_NAME}"
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  !insertmacro MUI_STARTMENU_WRITE_END

  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "SOFTWARE\${PRODUCT_NAME}"

  ; Remove files and uninstaller
  Delete "$INSTDIR\uninstall.exe"
  Delete "$INSTDIR\${EXECUTABLE_NAME}"
  Delete "$INSTDIR\${EXECUTABLE_NAME}.config"
  Delete "$INSTDIR\NAudio.dll"
  Delete "$INSTDIR\${HELP_FILE}"

  ; Remove shortcuts, if any
  !insertmacro MUI_STARTMENU_GETFOLDER StartMenuPageId $MUI_TEMP
    
  Delete "$SMPROGRAMS\$MUI_TEMP\Uninstall.lnk"
  Delete "$SMPROGRAMS\$MUI_TEMP\${PRODUCT_NAME}.lnk"
  
  ;Delete empty start menu parent diretories
  StrCpy $MUI_TEMP "$SMPROGRAMS\$MUI_TEMP"
 
  startMenuDeleteLoop:
	ClearErrors
    RMDir $MUI_TEMP
    GetFullPathName $MUI_TEMP "$MUI_TEMP\.."
    
    IfErrors startMenuDeleteLoopDone
  
    StrCmp $MUI_TEMP $SMPROGRAMS startMenuDeleteLoopDone startMenuDeleteLoop
  startMenuDeleteLoopDone:

  DeleteRegKey /ifempty HKCU "Software\Mark Heath\${PRODUCT_NAME}"
  DeleteRegKey /ifempty HKCU "Software\Mark Heath"

  ; Remove directories used
  RMDir "$INSTDIR"

SectionEnd
